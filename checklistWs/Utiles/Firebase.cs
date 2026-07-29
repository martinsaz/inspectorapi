using System.Net.Http.Headers;
using System.Security.Claims;
using checklistWs.Models.Firebase;

//using checklist.Clases;
//using checklist.Models.Usuarios;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace checklistWs.Utiles
{
    public class Firebase
	{

		public static async Task<string> GetCadenaConexion(IConfiguration configuration, string idEmpresa)
		{
			string result = "Ok";
			try
			{
				var config = new FirebaseAuthConfig
				{
					ApiKey = configuration.GetValue<string>("fireBdata:fireApiKey"),
					AuthDomain = configuration.GetValue<string>("fireBdata:fireAuthDomain"),
					Providers = new FirebaseAuthProvider[]
					{
						new EmailProvider()
					}
				};

				var client = new FirebaseAuthClient(config);
				await client.SignInWithEmailAndPasswordAsync(
					configuration.GetValue<string>("fireBdata:fireUser"),
					configuration.GetValue<string>("fireBdata:fireClave")
				);

				var firebaseClient = new FirebaseClient(
					configuration.GetValue<string>("fireBdata:fireDatabaseUrl"),
					new FirebaseOptions
					{
						AuthTokenAsyncFactory = () => Task.FromResult(client.User.Credential.RefreshToken)
					});

				var conexionesFb = await firebaseClient
					.Child("Conexiones")
					.OnceAsync<object>();

				var usuariosFb = await firebaseClient
					.Child("Usuarios")
					.OnceAsync<object>();

				foreach (var itemU in usuariosFb)
				{
					var usuario = JsonConvert.DeserializeObject<UsuarioFB>(itemU.Object.ToString());
					if ((bool)usuario.status)
					{
						foreach (var itemC in conexionesFb)
						{
							if (idEmpresa == usuario.empresa.ToString().ToUpper())
							{
								var conexionFB = JsonConvert.DeserializeObject<FireBconn>(itemC.Object.ToString());
								if (conexionFB.Status == "1")
								{
									result = conexionFB.Cadena;
								}
							}
						}
					}
				}

				// Cerrar sesión después de la operación
				 client.SignOut();
			}
			catch (Exception ex)
			{
				if (ex.InnerException is FirebaseAuthException fbAutException)
				{
					result = fbAutException.Reason.ToString();
					switch (result)
					{
						case "EmailExists":
							return "Ese usuario ya existe.";
						case "UnknownEmailAddress":
							return "Usuario o clave incorrecto";
					}
				}
				else
				{
					return ex.Message;
				}
			}
			return result;
		}

	}
}
