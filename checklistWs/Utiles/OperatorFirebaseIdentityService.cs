using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;

namespace checklistWs.Utiles
{
    public sealed class OperatorFirebaseIdentityService
    {
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, OperatorVerificationSession> VerificationSessions = new();

        public OperatorFirebaseIdentityService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<OperatorFirebaseCreateResult> CreateOperatorAsync(
            string nombreCompleto,
            string correo,
            string password,
            string empresa,
            Guid idEmpresa,
            Guid idOperador,
            bool activo)
        {
            FirebaseAuthClient client = CreateAuthClient();
            global::Firebase.Auth.UserCredential credential = await client.CreateUserWithEmailAndPasswordAsync(
                correo.Trim().ToLowerInvariant(),
                password,
                nombreCompleto.Trim());

            try
            {
                bool verificationEmailSent = await TrySendVerificationEmailAsync(credential.User.Credential.IdToken);
                CacheVerificationSession(
                    credential.User.Uid,
                    correo,
                    credential.User.Credential.RefreshToken);

                await UpsertOperatorNodeAsync(credential.User.Uid, nombreCompleto, correo, empresa, idEmpresa, idOperador, activo, false);

                return new OperatorFirebaseCreateResult
                {
                    Uid = credential.User.Uid,
                    VerificationEmailSent = verificationEmailSent
                };
            }
            catch
            {
                await credential.User.DeleteAsync();
                throw;
            }
        }

        public async Task DeleteProvisionedOperatorAsync(string correo, string password, string uid)
        {
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            FirebaseAuthClient operatorClient = CreateAuthClient();
            try
            {
                global::Firebase.Auth.UserCredential credential = await operatorClient.SignInWithEmailAndPasswordAsync(
                    correo.Trim().ToLowerInvariant(),
                    password);

                await DeleteOperatorNodeAsync(uid);
                await credential.User.DeleteAsync();
            }
            catch
            {
                await DeleteOperatorNodeAsync(uid);
            }
        }

        public async Task UpdateOperatorNodeAsync(string uid, string nombreCompleto, string correo, string empresa, Guid idEmpresa, Guid idOperador, bool activo)
        {
            OperatorFirebaseNodeState? currentNode = await GetOperatorNodeStateAsync(uid);
            await UpsertOperatorNodeAsync(
                uid,
                nombreCompleto,
                correo,
                empresa,
                idEmpresa,
                idOperador,
                activo,
                currentNode?.EmailVerified);
        }

        public async Task UpdateOperatorVerificationAsync(string uid, bool emailVerified)
        {
            if (string.IsNullOrWhiteSpace(uid))
            {
                return;
            }

            FirebaseAuthClient client = await SignInSupportAsync();
            try
            {
                FirebaseClient firebase = new FirebaseClient(
                    _configuration.GetValue<string>("fireBdata:fireDatabaseUrl"),
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(client.User.Credential.IdToken)
                    });

                await firebase
                    .Child("Operadores")
                    .Child(uid)
                    .Child("emailVerificado")
                    .PutAsync(emailVerified);

                if (emailVerified)
                {
                    VerificationSessions.TryRemove(uid, out _);
                }
            }
            finally
            {
                client.SignOut();
            }
        }

        public async Task<Dictionary<string, OperatorFirebaseNodeState>> GetOperatorNodeStatesAsync(IEnumerable<string> uids)
        {
            List<string> normalizedUids = uids
                .Where(uid => !string.IsNullOrWhiteSpace(uid))
                .Select(uid => uid.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!normalizedUids.Count.Equals(0))
            {
                FirebaseAuthClient client = await SignInSupportAsync();
                try
                {
                    FirebaseClient firebase = new FirebaseClient(
                        _configuration.GetValue<string>("fireBdata:fireDatabaseUrl"),
                        new FirebaseOptions
                        {
                            AuthTokenAsyncFactory = () => Task.FromResult(client.User.Credential.IdToken)
                        });

                    List<Task<OperatorFirebaseNodeState?>> tasks = normalizedUids
                        .Select(uid => firebase
                            .Child("Operadores")
                            .Child(uid)
                            .OnceSingleAsync<OperatorFirebaseNodeState>())
                        .ToList();

                    OperatorFirebaseNodeState?[] values = await Task.WhenAll(tasks);
                    Dictionary<string, OperatorFirebaseNodeState> result = new(StringComparer.OrdinalIgnoreCase);
                    for (int index = 0; index < normalizedUids.Count; index += 1)
                    {
                        if (values[index] != null)
                        {
                            result[normalizedUids[index]] = values[index]!;
                        }
                    }

                    return result;
                }
                finally
                {
                    client.SignOut();
                }
            }

            return new Dictionary<string, OperatorFirebaseNodeState>(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<OperatorVerificationResendResult> ResendVerificationAsync(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
            {
                return OperatorVerificationResendResult.Unavailable;
            }

            OperatorFirebaseNodeState? nodeState = await GetOperatorNodeStateAsync(uid);
            if (nodeState?.EmailVerified == true)
            {
                VerificationSessions.TryRemove(uid, out _);
                return OperatorVerificationResendResult.AlreadyVerified;
            }

            if (!VerificationSessions.TryGetValue(uid, out OperatorVerificationSession? verificationSession))
            {
                return OperatorVerificationResendResult.Unavailable;
            }

            if (verificationSession.ExpiresAtUtc <= DateTime.UtcNow)
            {
                VerificationSessions.TryRemove(uid, out _);
                return OperatorVerificationResendResult.Unavailable;
            }

            string? idToken = await ExchangeRefreshTokenForIdTokenAsync(verificationSession.RefreshToken);
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return OperatorVerificationResendResult.Unavailable;
            }

            bool sent = await TrySendVerificationEmailAsync(idToken);
            if (!sent)
            {
                return OperatorVerificationResendResult.Unavailable;
            }

            verificationSession.LastSentAtUtc = DateTime.UtcNow;
            verificationSession.ExpiresAtUtc = DateTime.UtcNow.AddDays(7);
            VerificationSessions[uid] = verificationSession;
            return OperatorVerificationResendResult.Sent;
        }

        public async Task<string?> FindAdministrativeUidByEmailAsync(Guid idEmpresa, string correo)
        {
            string correoNormalizado = (correo ?? string.Empty).Trim().ToLowerInvariant();
            if (idEmpresa == Guid.Empty || string.IsNullOrWhiteSpace(correoNormalizado))
            {
                return null;
            }

            FirebaseAuthClient client = await SignInSupportAsync();
            try
            {
                FirebaseClient firebase = new FirebaseClient(
                    _configuration.GetValue<string>("fireBdata:fireDatabaseUrl"),
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(client.User.Credential.IdToken)
                    });

                string empresaObjetivo = await ResolveOperatorCompanyCodeAsync(firebase, idEmpresa, string.Empty);
                if (string.IsNullOrWhiteSpace(empresaObjetivo))
                {
                    return null;
                }

                IReadOnlyCollection<FirebaseObject<AdministrativeFirebaseNodeState>> usuarios = await firebase
                    .Child("Usuarios")
                    .OnceAsync<AdministrativeFirebaseNodeState>();

                List<string> coincidencias = usuarios
                    .Where(item =>
                        item.Object != null
                        && string.Equals(
                            item.Object.correo?.Trim(),
                            correoNormalizado,
                            StringComparison.OrdinalIgnoreCase)
                        && string.Equals(
                            item.Object.empresa?.Trim(),
                            empresaObjetivo,
                            StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(item.Key))
                    .Select(item => item.Key.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return coincidencias.Count == 1 ? coincidencias[0] : null;
            }
            finally
            {
                client.SignOut();
            }
        }

        public async Task RevokeSessionAsync(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
            {
                return;
            }

            FirebaseAuthClient client = await SignInSupportAsync();
            try
            {
                FirebaseClient firebase = new FirebaseClient(
                    _configuration.GetValue<string>("fireBdata:fireDatabaseUrl"),
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(client.User.Credential.IdToken)
                    });

                await firebase.Child("Tokens").Child(uid).DeleteAsync();
            }
            finally
            {
                client.SignOut();
            }
        }

        public async Task RemoveOperatorNodeAsync(string uid)
        {
            await DeleteOperatorNodeAsync(uid);
        }

        public async Task SendResetPasswordAsync(string correo)
        {
            FirebaseAuthClient client = CreateAuthClient();
            await client.ResetEmailPasswordAsync(correo.Trim().ToLowerInvariant());
        }

        public void CacheVerificationSession(string uid, string correo, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }

            VerificationSessions[uid.Trim()] = new OperatorVerificationSession
            {
                Uid = uid.Trim(),
                Correo = correo.Trim().ToLowerInvariant(),
                RefreshToken = refreshToken.Trim(),
                LastSentAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
            };
        }

        private FirebaseAuthClient CreateAuthClient()
        {
            FirebaseAuthConfig config = new FirebaseAuthConfig
            {
                ApiKey = _configuration.GetValue<string>("fireBdata:fireApiKey"),
                AuthDomain = _configuration.GetValue<string>("fireBdata:fireAuthDomain"),
                Providers = new FirebaseAuthProvider[]
                {
                    new EmailProvider()
                }
            };

            return new FirebaseAuthClient(config);
        }

        private async Task<FirebaseAuthClient> SignInSupportAsync()
        {
            FirebaseAuthClient client = CreateAuthClient();
            await client.SignInWithEmailAndPasswordAsync(
                _configuration.GetValue<string>("fireBdata:fireUser"),
                _configuration.GetValue<string>("fireBdata:fireClave"));
            return client;
        }

        private async Task<OperatorFirebaseNodeState?> GetOperatorNodeStateAsync(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
            {
                return null;
            }

            FirebaseAuthClient client = await SignInSupportAsync();

            try
            {
                FirebaseClient firebase = new FirebaseClient(
                    _configuration.GetValue<string>("fireBdata:fireDatabaseUrl"),
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(client.User.Credential.IdToken)
                    });

                return await firebase
                    .Child("Operadores")
                    .Child(uid)
                    .OnceSingleAsync<OperatorFirebaseNodeState>();
            }
            finally
            {
                client.SignOut();
            }
        }

        private async Task UpsertOperatorNodeAsync(string uid, string nombreCompleto, string correo, string empresa, Guid idEmpresa, Guid idOperador, bool activo, bool? emailVerified)
        {
            FirebaseAuthClient client = await SignInSupportAsync();

            try
            {
                FirebaseClient firebase = new FirebaseClient(
                    _configuration.GetValue<string>("fireBdata:fireDatabaseUrl"),
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(client.User.Credential.IdToken)
                    });

                string empresaOperador = await ResolveOperatorCompanyCodeAsync(firebase, idEmpresa, empresa);

                var payload = new
                {
                    CheckApp = 1,
                    correo = correo.Trim().ToLowerInvariant(),
                    empresa = empresaOperador,
                    fechahora = DateTime.UtcNow.ToString("O"),
                    nombre = nombreCompleto.Trim().ToUpperInvariant(),
                    status = activo,
                    emailVerificado = emailVerified,
                    telefono = string.Empty,
                    uid,
                    tipoCuenta = "Operador",
                    idOperador = idOperador.ToString()
                };

                await firebase
                    .Child("Operadores")
                    .Child(uid)
                    .PutAsync(payload);
            }
            finally
            {
                client.SignOut();
            }
        }

        private static async Task<string> ResolveOperatorCompanyCodeAsync(FirebaseClient firebase, Guid idEmpresa, string empresaFallback)
        {
            if (idEmpresa == Guid.Empty)
            {
                return empresaFallback.Trim().ToUpperInvariant();
            }

            IReadOnlyCollection<FirebaseObject<JObject>> conexiones = await firebase
                .Child("Conexiones")
                .OnceAsync<JObject>();

            string idEmpresaNormalizado = idEmpresa.ToString().Trim();
            foreach (FirebaseObject<JObject> conexion in conexiones)
            {
                string? empresaConexion = conexion.Object?["idEmpresa"]?.ToString();
                if (string.Equals(empresaConexion, idEmpresaNormalizado, StringComparison.OrdinalIgnoreCase))
                {
                    return conexion.Key.Trim().ToUpperInvariant();
                }
            }

            return empresaFallback.Trim().ToUpperInvariant();
        }

        private async Task<bool> TrySendVerificationEmailAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return false;
            }

            using HttpClient client = new HttpClient();
            string requestUri = "https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=" + _configuration.GetValue<string>("fireBdata:fireApiKey");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            StringContent content = new StringContent(
                "{\"requestType\":\"VERIFY_EMAIL\",\"idToken\":\"" + idToken + "\"}",
                Encoding.UTF8,
                "application/json");

            HttpResponseMessage response = await client.PostAsync(requestUri, content);
            return response.IsSuccessStatusCode;
        }

        private async Task<string?> ExchangeRefreshTokenForIdTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            using HttpClient client = new HttpClient();
            string requestUri = "https://securetoken.googleapis.com/v1/token?key=" + _configuration.GetValue<string>("fireBdata:fireApiKey");
            StringContent content = new StringContent(
                "grant_type=refresh_token&refresh_token=" + Uri.EscapeDataString(refreshToken.Trim()),
                Encoding.UTF8,
                "application/x-www-form-urlencoded");

            HttpResponseMessage response = await client.PostAsync(requestUri, content);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            JObject payload = JObject.Parse(responseBody);
            return payload["id_token"]?.ToString();
        }

        private async Task DeleteOperatorNodeAsync(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
            {
                return;
            }

            FirebaseAuthClient client = await SignInSupportAsync();

            try
            {
                FirebaseClient firebase = new FirebaseClient(
                    _configuration.GetValue<string>("fireBdata:fireDatabaseUrl"),
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(client.User.Credential.IdToken)
                    });

                await firebase.Child("Operadores").Child(uid).DeleteAsync();
                await firebase.Child("Tokens").Child(uid).DeleteAsync();
                VerificationSessions.TryRemove(uid, out _);
            }
            finally
            {
                client.SignOut();
            }
        }
    }

    public sealed class OperatorFirebaseCreateResult
    {
        public string Uid { get; init; } = string.Empty;
        public bool VerificationEmailSent { get; init; }
    }

    public sealed class OperatorFirebaseNodeState
    {
        public int CheckApp { get; set; }
        public string correo { get; set; } = string.Empty;
        public string empresa { get; set; } = string.Empty;
        public string fechahora { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public bool status { get; set; }
        public bool? emailVerificado { get; set; }
        public string telefono { get; set; } = string.Empty;
        public string uid { get; set; } = string.Empty;
        public string tipoCuenta { get; set; } = string.Empty;
        public string idOperador { get; set; } = string.Empty;

        public bool Status => status;
        public bool? EmailVerified => emailVerificado;
    }

    public sealed class AdministrativeFirebaseNodeState
    {
        public string correo { get; set; } = string.Empty;
        public string empresa { get; set; } = string.Empty;
    }

    public enum OperatorVerificationResendResult
    {
        Sent,
        AlreadyVerified,
        Unavailable
    }

    internal sealed class OperatorVerificationSession
    {
        public string Uid { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime LastSentAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }
}
