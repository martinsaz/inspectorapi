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
using Newtonsoft.Json;

namespace checklistWs.Controllers
{
	public class FirebaseController1
	{
		

		//private IConfiguration configuration;

		//public FirebaseController1(IConfiguration iConfig)
		//{
		//	configuration = iConfig;
		//}

		//public async Task<string> GetCadenaConexion(string idEmpresa)
		//{
		//	string result = "Ok";
		//	try
		//	{
		//		var config = new FirebaseAuthConfig
		//		{
		//			ApiKey = configuration.GetValue<string>("fireBdata:fireApiKey"),
		//			AuthDomain = configuration.GetValue<string>("fireBdata:fireAuthDomain"),
		//			Providers = new FirebaseAuthProvider[]
		//			{
		//		new EmailProvider()
		//			}
		//		};
		//		var client = new FirebaseAuthClient(config);
		//		await client.SignInWithEmailAndPasswordAsync(configuration.GetValue<string>("fireBdata:fireUser"), configuration.GetValue<string>("fireBdata:fireClave"));
		//		var firebaseClient = new FirebaseClient(
		//			 configuration.GetValue<string>("fireBdata:fireDatabaseUrl"),
		//			 new FirebaseOptions
		//			 {
		//				 AuthTokenAsyncFactory = () => Task.FromResult(client.User.Credential.RefreshToken)
		//			 });
		//		var conexionesFb = await firebaseClient
		//			.Child("Conexiones").Child(idEmpresa)
		//			.OnceAsync<object>();
		//		var usuariosFb = await firebaseClient
		//		.Child("Usuarios")
		//		.OnceAsync<object>();

		//		foreach (var itemU in usuariosFb)
		//		{
		//			if (itemU.Key == client.User.Uid)
		//			{
		//				UsuarioFB usuario = JsonConvert.DeserializeObject<UsuarioFB>(itemU.Object.ToString());
		//				if ((bool)usuario.status)
		//				{
		//					foreach (var itemC in conexionesFb)
		//					{
		//						if (itemC.Key.ToUpper() == usuario.empresa.ToString().ToUpper())
		//						{
		//							FireBconn conexionFB = JsonConvert.DeserializeObject<FireBconn>(itemC.Object.ToString());
		//							if (conexionFB.Status == "1")
		//							{
		//								return conexionFB.Cadena;
		//							}
		//							else
		//							{
		//								return "Su empresa no está activa. Lo siento.";
		//							}
		//						}
		//					}
		//				}
		//				else
		//				{
		//					return "Su usuario está inactivo. Contacte a su administrador.";
		//				}
		//				break;
		//			}
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		if (ex.InnerException is FirebaseAuthException)
		//		{
		//			var fbAutException = (FirebaseAuthException)ex.InnerException;
		//			result = fbAutException.Reason.ToString();
		//			switch (result)
		//			{
		//				case "EmailExists":
		//					return "Ese usuario ya existe.";
		//				case "UnknownEmailAddress":
		//					return "Usuario o clave incorrecto";
		//			}
		//		}
		//		else
		//		{
		//			return ex.Message;
		//		}
		//	}
		//	return result;
		//}
	}
}
