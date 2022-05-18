using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using OSIsoft.AF;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PIVisionURLAttributeIdentifier
{

    class SQLdata : DataTable
    {
        public DataTable PullVisionAttributesGUIDlist(string sqlserver)
        {
            DataTable dataTable = new DataTable();
            string connString = $@"Server={sqlserver};Database=PIVision;Integrated Security=true;MultipleActiveResultSets=true"; /*---> using integrated security*/
            /*string connString = $@"Server={sqlserver};Database=PIVision;User ID=XavierF;password=XavierF!!;MultipleActiveResultSets=true";*/ /*---> using SQL user*/

            string _query = string.Format(" SELECT a.[DisplayID],Name ,[Server] , FullDatasource  FROM [PIVision].[dbo].[DisplayDatasources]a, [PIVision].[dbo].[View_DisplayList]b where a.DisplayID=b.DisplayID  and FullDatasource like '%|%'");
            string query = string.Format("SELECT a.[DisplayID],b.Name ,[Server] , FullDatasource, b.EditorDisplay, b.COG FROM [PIVision].[dbo].[DisplayDatasources]a, [PIVision].[dbo].[View_Displays]b where a.DisplayID=b.DisplayID  and FullDatasource like '%|%' and EditorDisplay like '%value%'");
            string __query = string.Format("SELECT distinct a.[DisplayID],b.Name , b.EditorDisplay, CAST( b.COG AS NVARCHAR(MAX) ) as COG  FROM [PIVision].[dbo].[DisplayDatasources]a, [PIVision].[dbo].[View_Displays]b where a.DisplayID=b.DisplayID  and FullDatasource like '%|%' ");
           
            SqlConnection connection = new SqlConnection(connString);
            SqlCommand command = new SqlCommand(query, connection);
            connection.Open();
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);
            connection.Close();
            adapter.Dispose();

            return dataTable;
        }

        public DataTable FormatDatable_create_attribute_element_columns(DataTable datatable)
        {
            datatable.Columns.Add("AFDatabase", typeof(string));
            datatable.Columns.Add("AttributePath", typeof(string));
            datatable.Columns.Add("AFattributeName", typeof(string));
            datatable.Columns.Add("AFattributeGUID", typeof(string));         

            for (int i = 0; i < datatable.Rows.Count; i++)
            {
                string FullDataSource = datatable.Rows[i][3].ToString();
                if (FullDataSource.Contains("?"))
                {
                    /*  FullDataSource = FullDataSource.TrimStart('\\');*/
                    string[] subs = FullDataSource.Split('?');
                    string[] subs1 = FullDataSource.Split('\\');
                    string databasename = subs1[3];
                    string elementPath = subs[0];
                    string[] subs2 = FullDataSource.Split('|');
                    /*string attributeName = "|"+subs2[1].Split('?')[0];*/
                    string attributeName = subs[1].Substring(36); //removing initial 36 characters representing GUID + 1 character representing the pipe |
                    string attributePath = elementPath + attributeName;

                    datatable.Rows[i]["AFDatabase"] = databasename;
                    datatable.Rows[i]["AttributePath"] = attributePath;
                    datatable.Rows[i]["AFattributeName"] = attributeName;
                    datatable.Rows[i]["AFattributeGUID"] = subs[2];
                }
                else
                {
                    string[] subs1 = FullDataSource.Split('\\');
                    string databasename = subs1[3];
                    datatable.Rows[i]["AFDatabase"] = databasename;
                    datatable.Rows[i]["AttributePath"] = FullDataSource;
                    //WILL NEED TO IMPLEMENT elementPath + attributeName
                }
            }
            return datatable;
        }

        public void FormatDatatable_getSymbolconfig(DataTable datatable)
        {
            datatable.Columns.Add("SymbolNumber", typeof(string));
            datatable.Columns.Add("AFData_ID", typeof(string));
            datatable.Columns.Add("LabelType", typeof(string));
            datatable.Columns.Add("CustomLabel", typeof(string));
            List<String[]> Symbols_AFDataID = new List<String[]>();

            //filling the datatable with AFData_ID
            for (int i = 0; i < datatable.Rows.Count; i++)
            {
                XmlDocument xdoc = new XmlDocument();
                xdoc.LoadXml(datatable.Rows[i]["COG"].ToString());

                XmlNode root = xdoc.FirstChild;
                if (root.HasChildNodes)
                {
                    foreach (XmlNode node in root)
                    {
                        /*Console.WriteLine(node.Name);*/
                        if (node.Name == "Datasources")
                        {
                            if (node.HasChildNodes)
                            {
                                /*Console.WriteLine(node.InnerXml);*/
                                foreach (XmlNode childnode in node)
                                {
                                    string attribute = childnode.InnerXml.Split('\"')[1];
                                    string attributeName = attribute.Split('?')[0];
                                    string attributeGUID = attribute.Split('?')[1];
                                    string AFData_ID = childnode.OuterXml.Split('\"')[1];

                                    if (attributeGUID == datatable.Rows[i]["AFattributeGUID"].ToString())
                                    {
                                        datatable.Rows[i]["AFData_ID"] = AFData_ID;
                                      /*  Console.WriteLine(i + " " + datatable.Rows[i]["AFData_ID"]);*/
                                    }
                                    
                                }
                            }
                        }
                        if (node.Name == "Symbols")
                        {
                            if (node.HasChildNodes)
                            {
                                foreach (XmlNode childnode in node)
                                {
                                    string AFData_ID = childnode.InnerXml.Split('\"')[1];
                                    string SymbolNum = childnode.OuterXml.Split('\"')[1];
                                    string[] AFData_ID_SymbolNum = { AFData_ID, SymbolNum };
                                    Symbols_AFDataID.Add(AFData_ID_SymbolNum);
                                }
                            }
                        }
                    }
                }
                for (int j = 0; j < Symbols_AFDataID.Count; j++)
                {
                    /*                  string var1 = datatable.Rows[i]["AFData_ID"].ToString();
                                        string var2 = Symbols_AFDataID[j][0];*/
                    if (datatable.Rows[i]["AFData_ID"].ToString() == Symbols_AFDataID[j][0])
                    {
                        datatable.Rows[i]["SymbolNumber"] = Symbols_AFDataID[j][1];
                        /*string var3 = Symbols_AFDataID[j][1];*/
                        /*Console.WriteLine("Attribute: "+ datatable.Rows[i]["AFattributeName"] + "AFDataID: " + datatable.Rows[i]["AFData_ID"] + " SymbolNumber: " + datatable.Rows[i]["SymbolNumber"]);*/
                    }

                }
            }

           /* for (int i = 0; i < datatable.Rows.Count; i++)
            {
                XmlDocument xdoc = new XmlDocument();
                xdoc.LoadXml(datatable.Rows[i]["COG"].ToString());
                
 
                XmlNode root = xdoc.FirstChild;
                if (root.HasChildNodes)
                {
                    foreach (XmlNode node in root)
                    {
                        if (node.Name == "Symbols")
                        {
                            if (node.HasChildNodes)
                            {
                                foreach (XmlNode childnode in node)
                                {
                                    string AFData_ID = childnode.InnerXml.Split('\"')[1];
                                    string SymbolNum= childnode.OuterXml.Split('\"')[1];
                                    string[] AFData_ID_SymbolNum = { AFData_ID, SymbolNum };
                                    Symbols_AFDataID.Add(AFData_ID_SymbolNum);
                                }                          
                            }
                        }
                    }

                }
               
            }*/
           

            for (int i = 0; i < datatable.Rows.Count; i++)
            {
                datatable.Rows[i]["LabelType"] = getValueSymbolConfiguration(datatable.Rows[i]["EditorDisplay"].ToString(), datatable.Rows[i]["SymbolNumber"].ToString()).Item1;
/*                Console.WriteLine("Display: " + datatable.Rows[i]["Name"] + ", Attribute: " + datatable.Rows[i]["AFattributeName"] + " AFDataID: " + datatable.Rows[i]["AFData_ID"] + " SymbolNumber: " + datatable.Rows[i]["SymbolNumber"] + " ,symbolType: " + datatable.Rows[i]["LabelType"]);
*/              if (datatable.Rows[i]["LabelType"].ToString() == "C")
                {
                    datatable.Rows[i]["CustomLabel"] = getValueSymbolConfiguration(datatable.Rows[i]["EditorDisplay"].ToString(), datatable.Rows[i]["SymbolNumber"].ToString()).Item2;
                }
            }

        }

        public Tuple<string,string> getValueSymbolConfiguration(string editorDisplay, string SymbolNumber)
        {
            //return configuration of Value symbolNumber
            string valueSymbolConfig = null;
            string customName = null;
            JObject json = JObject.Parse(editorDisplay.ToString());
            foreach (var item in json["Symbols"])
            {
                if (item["SymbolType"].ToString() == "collection")
                {
                    var StencilSymbols = item["StencilSymbols"];
                    foreach (var obj in StencilSymbols)
                    {
                        /*util.WriteInYellow(obj.ToString());*/
                        var config = obj["Configuration"];
                        if (item["SymbolType"].ToString() == "value" && item["Name"].ToString() == SymbolNumber)
                        {
                            if (!config.ToString().Contains("NameType"))
                            {
                                valueSymbolConfig = "default";
                            }
                            else
                            {
                                valueSymbolConfig = config["NameType"].ToString();
                                if (valueSymbolConfig == "C")
                                {
                                    customName = config["CustomName"].ToString();
                                }
                            }
                        }                    
                    }
                }
                else
                {
                    var config = item["Configuration"];

                    if (item["SymbolType"].ToString() == "value" && item["Name"].ToString() == SymbolNumber)
                    {
                        if (!config.ToString().Contains("NameType"))
                        {
                            valueSymbolConfig = "default";
                        }
                        else
                        {
                            valueSymbolConfig = config["NameType"].ToString();
                            if (valueSymbolConfig == "C")
                            {
                                customName = config["CustomName"].ToString();
                            }
                        }
                    }
                }
            }
            return new Tuple<string, string>(valueSymbolConfig,customName);
        }
        public void TestingSQLConnection(string sqlserver)
        {
            string connString = $@"Server={sqlserver};Database=PIVision;Integrated Security=true;MultipleActiveResultSets=true"; /*---> using integrated security*/
            /* string connString = $@"Server={sqlserver};Database=PIVision;User ID=XavierF;password=XavierF!!;MultipleActiveResultSets=true";*/ /*---> using SQL user*/
            SqlConnection connection = new SqlConnection(connString);
            connection.Open();
        }

        public string ValidatingSQLConnection()
        {
            Utilities util = new Utilities();
            util.WriteInGreen("Enter the SQL Database instance hosting the PIVision database:");
            bool repeat = true;
            string sqlInstance = "";


            while (repeat)
            {
                Console.ForegroundColor = ConsoleColor.White;
                sqlInstance = Console.ReadLine();

                try
                {
                    util.WriteInYellow("Validating connection to the PIVision SQL database...");
                    TestingSQLConnection(sqlInstance);
                    repeat = false;
                }
                catch (SqlException ex)
                {
                    StringBuilder errorMessages = new StringBuilder();
                    util.WriteInRed("Could not connect to your PI Vision SQL database.");
                    for (int i = 0; i < ex.Errors.Count; i++)
                    {
                        errorMessages.Append("Index #" + i + "\n" +
                            "Message: " + ex.Errors[i].Message + "\n" +
                            "LineNumber: " + ex.Errors[i].LineNumber + "\n" +
                            "Source: " + ex.Errors[i].Source + "\n" +
                            "Procedure: " + ex.Errors[i].Procedure + "\n");
                    }
                    util.WriteInRed(errorMessages.ToString());
                    util.WriteInGreen("Something went wrong. Enter the SQL Database instance hosting the PIVision database:");
                    repeat = true;
                }
            }
            return sqlInstance;
        }
    }
}
