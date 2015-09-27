using FederatedSearch.Models;
using Microsoft.Practices.ServiceLocation;
using SolrNet;
using SolrNet.Commands.Parameters;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace FederatedSearch.Controllers
{
    public class Run
    {
        public String db = null;
        public int? queryIndex = null;
        public int? runId = null;
        public bool Success = true;
        public Exception error = null;
        public double timing = -1;
    }

    [Authorize]
    public class AdminController : Controller
    {
        public AdminController()
        {
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult RunTest(int iterations, String queryText)
        {
            var data = new List<Run>();
            
            var queries = queryText.Split(new char[]{ ';' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < queries.Length; i++)
            {
                // interleave, vs in sequence vs random?
                for (int j = 0; j < iterations; j++)
                {
                    data.Add(RunQuery("db1", i + 1, j + 1, queries[i]));
                    data.Add(RunQuery("db2", i + 1, j + 1, queries[i]));
                }
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        private Run RunQuery(String dbIdentifier, int queryIdentifier, int runId, string query)
        {
            Exception exception = null;

            DateTime start = DateTime.Now;

            // do work...
            try
            {
                System.Configuration.Configuration rootWebConfig =
                    System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/MyWebSiteRoot");

                System.Configuration.ConnectionStringSettings connString;

                connString =
                    rootWebConfig.ConnectionStrings.ConnectionStrings[dbIdentifier];
				
                SqlConnection myConn = new SqlConnection(connString.ConnectionString);
                try
                {
                    myConn.Open();

                    var transaction = myConn.BeginTransaction();
                    SqlCommand myCommand = new SqlCommand(query, myConn, transaction);

                    try
                    {
                        var data = myCommand.ExecuteReader();


                        while (data.NextResult())
                        {
                        }

                        data.Close();
                    }
                    catch (System.Exception ex)
                    {
                        if (exception == null)
                        {
                            exception = ex;
                        }
                    }
                    finally
                    {
                        transaction.Rollback();
                    }
                }
                catch (System.Exception ex)
                {
                    if (exception == null)
                    {
                        exception = ex;
                    }
                }
                finally
                {
                    if (myConn.State == ConnectionState.Open)
                        myConn.Close();
                }
            }
            catch (Exception e)
            {
                if (exception == null)
                {
                    exception = e;
                }
            }


            DateTime end = DateTime.Now;

            return new Run
            {
                db = dbIdentifier,
                queryIndex = queryIdentifier,
                runId = runId,
                Success = (exception == null),
                error = exception,
                timing = (end - start).TotalMilliseconds
            };
        }    
    }
}
