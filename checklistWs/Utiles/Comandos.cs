using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

namespace Sergio.Utiles
{
    public class Comandos
    {


        public static Boolean ExisteElemento(string query, string cnn)
        {
            Boolean existe = false;
            DataTable dt = MyDataTable(query, cnn);

            if (dt.Rows.Count > 0)
            {
                existe = true;
            }
            return existe;
        }


        public static SqlDataReader MyDataReader(string query, string cnn)
        {
            SqlConnection CNN = new SqlConnection(cnn);
            SqlCommand cmm = new SqlCommand(query, CNN);
            cmm.CommandTimeout = 0;
            SqlDataReader dr;

            try
            {
                CNN.Open();
                dr = cmm.ExecuteReader();
            }
            catch (Exception ex)
            {
                dr = null;
            }
            return dr;
        }

        public static string Executa(string query, string cnn, string tabla = "", string Elemento = "")
        {
            string Resultado = "";
            SqlConnection CNN = new SqlConnection(cnn);
            try
            {
                CNN.Open();
                SqlCommand cmm = new SqlCommand(query, CNN);
                cmm.CommandTimeout = 0;
                cmm.ExecuteNonQuery();
                CNN.Close();
            }
            catch (Exception ex)
            {
                CNN.Close();
                Resultado = ex.Message;
            }
            return Resultado;
        }


        public static DataSet MyDataSet(string query, string cnn)
        {
            SqlConnection CNN = new SqlConnection(cnn);
            DataSet ds = new DataSet();
            try
            {
                CNN.Open();
                SqlDataAdapter da = new SqlDataAdapter(query, CNN);
                da.SelectCommand.CommandTimeout = 0;
                da.Fill(ds);
                CNN.Close();
            }
            catch (Exception ex)
            {
                CNN.Close();
            }
            return ds;
        }

        public static DataTable MyDataTable(string query, string cnn)
        {
            SqlConnection CNN = new SqlConnection(cnn);
            DataTable dt = new DataTable();
            try
            {
                CNN.Open();
                SqlDataAdapter da = new SqlDataAdapter(query, CNN);
                da.SelectCommand.CommandTimeout = 0;
                da.Fill(dt);
                CNN.Close();
            }
            catch (Exception ex)
            {
                CNN.Close();
                dt = null;
            }
            return dt;
        }

        public static Object ExecutaID(string query, string cnn)
        {
            Object id = new Object();
            SqlConnection CNN = new SqlConnection(cnn);
            try
            {
                CNN.Open();
                SqlCommand cmm = new SqlCommand(query, CNN);
                cmm.CommandTimeout = 0;
                id = cmm.ExecuteScalar();
                CNN.Close();
            }
            catch (Exception ex)
            {
                CNN.Close();
                id = ex.Message;
            }

            if (id == null)
            {
                id = "-1";
            }


            return id;
        }

        public static Boolean ExecutaSqlList(List<string> querys, string cnn)
        {
            Boolean regresa = false;
            SqlConnection CNN = new SqlConnection(cnn);
            SqlTransaction MyTrans;

            CNN.Open();

            SqlCommand cmm = CNN.CreateCommand();
            cmm.CommandTimeout = 0;
            cmm.Connection = CNN;
            MyTrans = CNN.BeginTransaction(IsolationLevel.ReadUncommitted);
            cmm.Transaction = MyTrans;
            int actual = 0;
            String qryErr = "";
            try
            {
                foreach (string qry in querys)
                {
                    qryErr = qry;
                    actual++;
                    cmm.CommandText = qry;
                    cmm.ExecuteNonQuery();
                }
                MyTrans.Commit();
                regresa = true;
            }
            catch (Exception ex)
            {
                MyTrans.Rollback();
            }
            finally
            {
                CNN.Close();
            }
            return regresa;
        }





        public static Boolean EsNumero(String Dato)
        {
            Boolean esNumero = false;
            double ejem = 0.00;
            if (double.TryParse(Dato, out ejem))
            {
                esNumero = true;
            }
            return esNumero;
        }

        // Aqui termina la Clase
    }
}
