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
using System.IO;

namespace PIVisionURLAttributeIdentifier
{

    class SQLdata : DataTable
    {
        public DataTable PullVisionAttributesGUIDlist(string sqlserver)
        {
            DataTable dataTable = new DataTable();
            string connString = $@"Server={sqlserver};Database=PIVision;Integrated Security=true;MultipleActiveResultSets=true"; /*---> using integrated security*/
            /*string connString = $@"Server={sqlserver};Database=PIVision;User ID=XavierF;password=XavierF!!;MultipleActiveResultSets=true";*/ /*---> using SQL user*/
          
            string query = File.ReadAllText(@"..\..\Queries\query.sql");

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
            datatable.Columns.Add("LabelType", typeof(string));
            datatable.Columns.Add("CustomLabel", typeof(string));

            //create temp table and inject it into master DT
            DataTable tempdatatable = new DataTable();
            tempdatatable.Columns.Add("DisplayID", typeof(string));
            tempdatatable.Columns.Add("Name", typeof(string));
            tempdatatable.Columns.Add("Server", typeof(string));
            tempdatatable.Columns.Add("FullDatasource", typeof(string));
            tempdatatable.Columns.Add("EditorDisplay", typeof(string));
            tempdatatable.Columns.Add("COG", typeof(string));
            tempdatatable.Columns.Add("Collection", typeof(string));
            tempdatatable.Columns.Add("AFDatabase", typeof(string));
            tempdatatable.Columns.Add("AttributePath", typeof(string));
            tempdatatable.Columns.Add("AFattributeName", typeof(string));
            tempdatatable.Columns.Add("AFattributeGUID", typeof(string));
            tempdatatable.Columns.Add("LabelType", typeof(string));
            tempdatatable.Columns.Add("CustomLabel", typeof(string));

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
                    string attributeName = subs[1].Substring(36); //removing initial 36 characters representing GUID
                    string attributePath = elementPath + attributeName;

                    datatable.Rows[i]["AFDatabase"] = databasename;
                    datatable.Rows[i]["AttributePath"] = attributePath;
                    datatable.Rows[i]["AFattributeName"] = attributeName;

                    string attrGUID = FullDataSource.Substring(FullDataSource.Length - 36);
                    datatable.Rows[i]["AFattributeGUID"] = attrGUID;
                }
                else // collection
                {
                    List<List<string>> attDetails =getCollectionAttributeDetails(datatable.Rows[i]["EditorDisplay"].ToString());
                    /*var attguid = attDetails[0][4];*/
                    datatable.Rows[i]["Server"] = attDetails[0][0];  
                    datatable.Rows[i]["AFDatabase"] = attDetails[0][1];
                    datatable.Rows[i]["AttributePath"] = attDetails[0][2];
                    datatable.Rows[i]["AFattributeName"] = attDetails[0][3];
                    datatable.Rows[i]["AFattributeGUID"] = attDetails[0][4];
                    datatable.Rows[i]["LabelType"] = attDetails[0][5];
                    datatable.Rows[i]["CustomLabel"] = attDetails[0][6];

                    attDetails.Remove(attDetails[0]);
                    //EITHER DELETE INITIAL ROW OR EDIT INITIAL ROW AND LOOP FROM i=1 INSTEAD OF FOREACH


                    foreach (List<string> attList in attDetails)
                    {                       
                        DataRow row = tempdatatable.NewRow();
                        row["DisplayID"] = datatable.Rows[i]["DisplayID"];
                        row["Name"] = datatable.Rows[i]["Name"];
                        row["Server"] = attList[0];
                        row["FullDatasource"] = "";
                        row["EditorDisplay"] = "";
                        row["COG"] = "";
                        row["Collection"] = "1";
                        row["AFDatabase"] = attList[1]; ;
                        row["AttributePath"] = attList[2];
                        row["AFattributeName"] = attList[3];
                        row["AFattributeGUID"] = attList[4];
                        row["LabelType"]=attList[5]; ;
                        row["CustomLabel"]= attList[6]; ;

                        tempdatatable.Rows.Add(row);
                    }
                }

            }
            foreach (DataRow newrow in tempdatatable.Rows)
            {
                DataRow row = datatable.NewRow();
                row["DisplayID"] = newrow["DisplayID"];
                row["Name"] = newrow["Name"];
                row["Server"] = newrow["Server"];
                row["FullDatasource"] = newrow["FullDatasource"];
                row["EditorDisplay"] = newrow["EditorDisplay"];
                row["COG"] = newrow["COG"];
                row["Collection"] = newrow["Collection"];
                row["AFDatabase"] = newrow["AFDatabase"];
                row["AttributePath"] = newrow["AttributePath"];
                row["AFattributeName"] = newrow["AFattributeName"];
                row["AFattributeGUID"] = newrow["AFattributeGUID"];
                row["LabelType"] = newrow["LabelType"];
                row["CustomLabel"] = newrow["CustomLabel"];
                datatable.Rows.Add(row);
            }
            return datatable;
        }

        public List<List<string>> getCollectionAttributeDetails(string editorDisplay)
        {
            List<List<string>> attributesInCollection = new List<List<string>>();
            

            JObject json = JObject.Parse(editorDisplay.ToString());
            foreach (var item in json["Symbols"])
            {
                if (item["SymbolType"].ToString() == "collection")
                {
                    var StencilSymbols = item["StencilSymbols"];
                    foreach (var obj in StencilSymbols)
                    {
                        if (obj.ToString().Contains("DataSources"))
                        {
                            List<string> attributeDetails = new List<string>();
                            /*util.WriteInYellow(obj.ToString());*/
                            string FullDataSource = obj["DataSources"][0].ToString();

                            string[] subs = FullDataSource.Split('?');

                            string elementPath = subs[0].Substring(3); //removing initial 3 characters "af:"
                            string[] subs1 = FullDataSource.Split('\\');
                            string server = subs1[2]; ;
                            string databasename = subs1[3];

                            /*string attributeName = "|"+subs2[1].Split('?')[0];*/
                            string attributeName = subs[1].Substring(36); //removing initial 36 characters representing GUID
                            string attributePath = elementPath + attributeName;
                            string afattGUID = subs[1].Split('|')[0];

                            attributeDetails.Add(server);
                            attributeDetails.Add(databasename);
                            attributeDetails.Add(attributePath);
                            attributeDetails.Add(attributeName);
                            attributeDetails.Add(afattGUID);

                            var config = obj["Configuration"];
                            var symboltype = obj["SymbolType"].ToString();
                            var itemName = item["Name"].ToString();
                            string labeltype = "";
                            string customName = "";
                            if (obj["SymbolType"].ToString() == "value")
                            {
                                var datasource = obj["DataSources"];

                                if (!config.ToString().Contains("NameType"))
                                {
                                    labeltype = "F (collection)";
                                    attributeDetails.Add(labeltype);
                                    attributeDetails.Add(customName);
                                }
                                else
                                {
                                    labeltype = config["NameType"].ToString() + " (collection)";
                                    attributeDetails.Add(labeltype);

                                    if (labeltype == "C")
                                    {
                                        customName = config["CustomName"].ToString();
                                        attributeDetails.Add(customName);
                                    }
                                    else
                                    {
                                        attributeDetails.Add(customName);
                                    }
                                }
                            }
                            attributesInCollection.Add(attributeDetails);
                        }
                      
                        
                    }
                }
            }
            return attributesInCollection;
        }

        public void FormatDatatable_getSymbolconfig(DataTable datatable)
        {
            datatable.Columns.Add("SymbolNumber", typeof(string));
            datatable.Columns.Add("AFData_ID", typeof(string));
            List<String[]> Symbols_AFDataID = new List<String[]>();

            //filling the datatable with AFData_ID
            for (int i = 0; i < datatable.Rows.Count; i++)
            {
                if (datatable.Rows[i]["Collection"].ToString()=="0") // if symbol not part of a collection
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
                                        if (!attribute.Contains("*")&&!!attribute.Contains("?"))
                                        {
                                            string attributeName = attribute.Split('?')[0];
                                            string attributeGUID = attribute.Split('?')[1];
                                            string AFData_ID = childnode.OuterXml.Split('\"')[1];

                                            string afguid = datatable.Rows[i]["AFattributeGUID"].ToString();
                                            if (attributeGUID == datatable.Rows[i]["AFattributeGUID"].ToString())
                                            {
                                                datatable.Rows[i]["AFData_ID"] = AFData_ID;
                                                /*  Console.WriteLine(i + " " + datatable.Rows[i]["AFData_ID"]);*/
                                            }
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

            }
           
            for (int i = 0; i < datatable.Rows.Count; i++)
            {
                if (datatable.Rows[i]["Collection"].ToString() == "0")
                {
                    datatable.Rows[i]["LabelType"] = getValueSymbolConfiguration(datatable.Rows[i]["EditorDisplay"].ToString(), datatable.Rows[i]["SymbolNumber"].ToString()).Item1;

                    if (datatable.Rows[i]["LabelType"].ToString() == "C")
                    {
                        datatable.Rows[i]["CustomLabel"] = getValueSymbolConfiguration(datatable.Rows[i]["EditorDisplay"].ToString(), datatable.Rows[i]["SymbolNumber"].ToString()).Item2;
                    }
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
                        var symboltype = obj["SymbolType"].ToString();
                        var itemName = item["Name"].ToString();
                        if (obj["SymbolType"].ToString() == "value" && item["Name"].ToString() == SymbolNumber)
                        {
                            if (!config.ToString().Contains("NameType"))
                            {
                                valueSymbolConfig = "F (collection)";
                            }
                            else
                            {
                                valueSymbolConfig = config["NameType"].ToString()+" (collection)";
                                if (valueSymbolConfig == "C")
                                {
                                    if (config.ToString().Contains("CustomName"))
                                    {
                                        customName = config["CustomName"].ToString();
                                    }                                       
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
                            valueSymbolConfig = "F";
                        }
                        else
                        {
                            valueSymbolConfig = config["NameType"].ToString();
                            if (valueSymbolConfig == "C")
                            {
                                if (config.ToString().Contains("CustomName"))
                                {
                                    customName = config["CustomName"].ToString();
                                }
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
