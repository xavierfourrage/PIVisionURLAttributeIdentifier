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
                    
            string query = File.ReadAllText(@".\Queries\query.sql");

            SqlConnection connection = new SqlConnection(connString);
            SqlCommand command = new SqlCommand(query, connection);
            connection.Open();
            SqlDataAdapter adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);
            connection.Close();
            adapter.Dispose();

            return dataTable;
        }

        public DataTable formatDTandAddRowBasedOnCOGandEditorDisplay(DataTable DT)
        {
            DataTable attTable = new AFAttributeTable().createDT();

            foreach (DataRow r in DT.Rows)
            {
                //inject DisplayID + DisplayName
                DataTable dt=readCOG(r["COG"].ToString());
                foreach( DataRow newrow in dt.Rows)
                {
                    DataRow row = attTable.NewRow();
                    row["DisplayID"] = r["DisplayID"];
                    row["Name"] = r["Name"];
                    row["Server"] = newrow["Server"];                  
                    row["EditorDisplay"] = r["EditorDisplay"];
                    row["COG"] = r["COG"];                  
                    row["AFDatabase"] = newrow["AFDatabase"];
                    row["AttributePath"] = newrow["AttributePath"];
                    row["AFattributeName"] = newrow["AFattributeName"];
                    row["AFattributeGUID"] = newrow["AFattributeGUID"];
                    row["SymbolNum"] = newrow["SymbolNum"];
                    row["LabelType"] = "";
                    row["CustomLabel"] = "";
                    row["ShowValue"] = "";
                    row["ShowLabel"] = "";
                    attTable.Rows.Add(row);

                }
                if (r["EditorDisplay"].ToString().Contains("StencilSymbols")) //checking if Display contains collection
                {
                    List<List<string>> attDetails = getCollectionAttributeDetails(r["EditorDisplay"].ToString());
                    
                    foreach (List<string> l in attDetails)
                    {
                        DataRow row = attTable.NewRow();
                        row["DisplayID"] = r["DisplayID"];
                        row["Name"] = r["Name"];
                        row["Server"] = l[0];
                        row["EditorDisplay"] = r["EditorDisplay"];
                        row["COG"] = r["COG"];
                        row["AFDatabase"] = l[1];
                        row["AttributePath"] = l[2];
                        row["AFattributeName"] = l[3];
                        row["AFattributeGUID"] = l[4];
                        row["SymbolNum"] = "";
                        row["ShowValue"] = l[5];
                        row["ShowLabel"] = l[6];
                        row["LabelType"] = l[7];
                        row["CustomLabel"] = l[8];

                        attTable.Rows.Add(row);
                    }
                }               
            }
            return attTable;
        }

        public DataTable readCOG(string COG)
        {
            IDictionary<string, string[]> AFInfo = new Dictionary<string, string[]>(); //AF_ID,(AF Server, AF Database)
            List<string[]> AFData_ID_SymbolNum = new List<string[]>(); //AFData_ID_SymbolNumber

            IDictionary<string, string[]> AttributeProperties = new Dictionary<string, string[]>(); //(AFData_ID,attpropList);

            DataTable attributeTable = new DataTable();
            attributeTable.Columns.Add("Server", typeof(string));      
            attributeTable.Columns.Add("AFDatabase", typeof(string));
            attributeTable.Columns.Add("AttributePath", typeof(string));
            attributeTable.Columns.Add("AFattributeName", typeof(string));
            attributeTable.Columns.Add("AFattributeGUID", typeof(string));
            attributeTable.Columns.Add("AFData_ID", typeof(string));
            attributeTable.Columns.Add("dBRef", typeof(string));
            attributeTable.Columns.Add("SymbolNum", typeof(string));

            XmlDocument xdoc = new XmlDocument();
                    xdoc.LoadXml(COG);

                    XmlNode root = xdoc.FirstChild;
                    if (root.HasChildNodes)
                    {
                        foreach (XmlNode node in root)
                        {
                            if (node.Name == "Databases")
                            {
                                if (node.HasChildNodes)
                                {                               
                                    foreach (XmlNode childnode in node)
                                    {
                                        if (!childnode.OuterXml.Contains("<PI ID="))
                                        {
                                            /* <AF Id="AF_402655" Node="CSAF" Db="Wells" />*/
                                            var afInfo = childnode.OuterXml.Split('\"');
                                            string AF_Id = afInfo[1];
                                            string AFServer = afInfo[3];
                                            string AFDatabase = afInfo[5];

                                            string[] af_array = { AFServer, AFDatabase };
                                            AFInfo.Add(AF_Id, af_array);
                                        }
                                    }
                                }
                            }
                            /*Console.WriteLine(node.Name);*/
                            if (node.Name == "Datasources")
                            {
                                if (node.HasChildNodes)
                                {
                                    /*Console.WriteLine(node.InnerXml);*/
                                    foreach (XmlNode childnode in node)
                                    {
                                        if (childnode.OuterXml.Contains("AFData"))
                                        {
                                            string attribute = childnode.InnerXml.Split('\"')[1];
                                            if (attribute.Contains('?'))
                                            {
                                                if (!attribute.Contains("*"))
                                                {
                                                    string attributeName = attribute.Split('?')[0];
                                                    string attributeGUID = attribute.Split('?')[1];
                                                    string AFData_ID = childnode.OuterXml.Split('\"')[1]; /*    <AFData Id="AF_57399" DbRef="AF_402655">*/
                                                    string dBRef = childnode.OuterXml.Split('\"')[3];
                                                    string elementPath = childnode.InnerXml.Split('\"')[3].Split('?')[0];
                                                    string attributePath = elementPath + "|" + attributeName;

                                                    string[] attpropList = { attributePath, attributeName, attributeGUID, dBRef };
                                                    AttributeProperties.Add(AFData_ID, attpropList);
                                                }
                                            }
                                    /*      <AFAttribute Name="Reactor - Operating Windows Parameters (Alarm)" ElementPath="FCC\PAR" />*/
                                            else
                                            {
                                                string attributeName = attribute.Split('?')[0];
                                                string attributeGUID = "";
                                                    string AFData_ID = childnode.OuterXml.Split('\"')[1]; /*    <AFData Id="AF_57399" DbRef="AF_402655">*/
                                                    string dBRef = childnode.OuterXml.Split('\"')[3];
                                                    string elementPath = childnode.InnerXml.Split('\"')[3];
                                                    string attributePath = elementPath + "|" + attributeName;

                                                    string[] attpropList = { attributePath, attributeName, attributeGUID, dBRef };
                                                    AttributeProperties.Add(AFData_ID, attpropList);
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
                                        string[] list = { AFData_ID, SymbolNum };

                                        AFData_ID_SymbolNum.Add(list);
                                      
                                    }
                                }
                            }                            
                        }
                    }
                
                    for(int i=0; i< AFData_ID_SymbolNum.Count;i++)
                    {
                        if (AttributeProperties.TryGetValue(AFData_ID_SymbolNum[i][0], out string[] attproptList ))
                        {

                        DataRow row = attributeTable.NewRow();

                        row["AttributePath"] = attproptList[0];
                        row["AFattributeName"] = attproptList[1];
                        row["AFattributeGUID"] = attproptList[2];                       
                         row["dBRef"] = attproptList[3];
                         row["AFData_ID"] = AFData_ID_SymbolNum[i][0];
                        row["SymbolNum"] = AFData_ID_SymbolNum[i][1];
                        attributeTable.Rows.Add(row);

                        }
                    }
                    for (int j = 0; j < attributeTable.Rows.Count; j++)
                    {

                        if (AFInfo.TryGetValue(attributeTable.Rows[j]["dBRef"].ToString(), out string[] AFServer_AFDatabase))
                        {
                            attributeTable.Rows[j]["Server"] = AFServer_AFDatabase[0];
                            attributeTable.Rows[j]["AFDatabase"] = AFServer_AFDatabase[1];
                        }
                    }

            return attributeTable;
        }
        public DataTable UpdateDTwithValueSymbolConfig(DataTable dt)
        {
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["SymbolNum"].ToString() != "") // only editing non collection symbols since those have already been scanned in formatDTandAddRowBasedOnCOGandEditorDisplay
                {
                    List<string> valueSymbolConfig = getValueSymbolConfiguration(dr["EditorDisplay"].ToString(), dr["SymbolNum"].ToString());
                    dr["LabelType"] = valueSymbolConfig[0];
                    dr["CustomLabel"] = valueSymbolConfig[1];
                    dr["ShowValue"] = valueSymbolConfig[2];
                    dr["ShowLabel"] = valueSymbolConfig[3];
                }             
            }
            RemoveNullColumnFromDataTable(dt);
            return dt; 
        }

        public static void RemoveNullColumnFromDataTable(DataTable dt)
        {
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if (dt.Rows[i]["LabelType"] == DBNull.Value)
                    dt.Rows[i].Delete();
            }
            dt.AcceptChanges();
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
            DataTable attributeTable = new DataTable();
            attributeTable.Columns.Add("DisplayID", typeof(string));
            attributeTable.Columns.Add("Name", typeof(string));
            attributeTable.Columns.Add("Server", typeof(string));
            attributeTable.Columns.Add("FullDatasource", typeof(string));
            attributeTable.Columns.Add("EditorDisplay", typeof(string));
            attributeTable.Columns.Add("COG", typeof(string));
            attributeTable.Columns.Add("Collection", typeof(string));
            attributeTable.Columns.Add("AFDatabase", typeof(string));
            attributeTable.Columns.Add("AttributePath", typeof(string));
            attributeTable.Columns.Add("AFattributeName", typeof(string));
            attributeTable.Columns.Add("AFattributeGUID", typeof(string));
            attributeTable.Columns.Add("LabelType", typeof(string));
            attributeTable.Columns.Add("CustomLabel", typeof(string));

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
                        DataRow row = attributeTable.NewRow();
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

                        attributeTable.Rows.Add(row);
                    }
                }
            }
            foreach (DataRow newrow in attributeTable.Rows)
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
                        if (obj.ToString().Contains("DataSources") && obj.ToString().Contains("\"SymbolType\": \"value\""))
                        {
                            List<string> attributeDetails = new List<string>();
                            /*util.WriteInYellow(obj.ToString());*/
                            string FullDataSource = obj["DataSources"][0].ToString();

                            if (FullDataSource.Contains("?"))
                            {
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
                            }
                            else
                            {
                                string attributePath = FullDataSource.Substring(3); //removing initial 3 characters "af:"
                                string[] subs1 = FullDataSource.Split('\\');
                                string server = subs1[2]; ;
                                string databasename = subs1[3];
/*                              string attributeName = FullDataSource.Split('|')[1];
*/                              string attributeName = FullDataSource.Split('|')[FullDataSource.Split('|').Length-1];
                                string afattGUID = "noGUID";
                                attributeDetails.Add(server);
                                attributeDetails.Add(databasename);
                                attributeDetails.Add(attributePath);
                                attributeDetails.Add(attributeName);
                                attributeDetails.Add(afattGUID);
                            }
                            
                            var config = obj["Configuration"];
/*                          var symboltype = obj["SymbolType"].ToString();
                            var itemName = item["Name"].ToString();*/
                            string labeltype = "";
                            string customName = "";
                            if (obj["SymbolType"].ToString() == "value")
                            {
                                attributeDetails.Add(config["ShowValue"].ToString());
                                attributeDetails.Add(config["ShowLabel"].ToString());

                                if (!config.ToString().Contains("NameType"))
                                {
                                    labeltype = "F (col)";
                                    attributeDetails.Add(labeltype);
                                    attributeDetails.Add(customName);
                                }
                                else
                                {
                                    labeltype = config["NameType"].ToString() + " (col)";
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
                        if (datatable.Rows[i]["AFData_ID"].ToString() == Symbols_AFDataID[j][0])
                        {
                            datatable.Rows[i]["SymbolNumber"] = Symbols_AFDataID[j][1];
            
                        }
                    }
                }

            }
           
            for (int i = 0; i < datatable.Rows.Count; i++)
            {
                if (datatable.Rows[i]["Collection"].ToString() == "0")
                {
                    datatable.Rows[i]["LabelType"] = getValueSymbolConfiguration(datatable.Rows[i]["EditorDisplay"].ToString(), datatable.Rows[i]["SymbolNumber"].ToString())[0];

                    if (datatable.Rows[i]["LabelType"].ToString() == "C")
                    {
                        datatable.Rows[i]["CustomLabel"] = getValueSymbolConfiguration(datatable.Rows[i]["EditorDisplay"].ToString(), datatable.Rows[i]["SymbolNumber"].ToString())[1];
                    }
                }
                   
            }
        }
        public List<string> getValueSymbolConfiguration(string editorDisplay, string SymbolNumber)
        {
            //return configuration of Value symbolNumber
            string valueSymbolConfig = null;
            string customName = null;
            string ShowValue = null;
            string ShowLabel = null;
            List<string> valueSymbolConfiguration = new List<string>();
            JObject json = JObject.Parse(editorDisplay.ToString());
            foreach (var item in json["Symbols"])
            {
                    var config = item["Configuration"];

                    if (item["SymbolType"].ToString() == "value" && item["Name"].ToString() == SymbolNumber)
                    {
                        ShowValue = config["ShowValue"].ToString();
                        ShowLabel = config["ShowLabel"].ToString();
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
            valueSymbolConfiguration.Add(valueSymbolConfig);
            valueSymbolConfiguration.Add(customName);
            valueSymbolConfiguration.Add(ShowValue);
            valueSymbolConfiguration.Add(ShowLabel);
            return valueSymbolConfiguration;
        }
        public void TestingSQLConnection(string sqlserver)
        {
            string connString = $@"Server={sqlserver};Database=PIVision;Integrated Security=true;MultipleActiveResultSets=true"; /*---> using integrated security*/
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
                    util.WriteInGray("Validating connection to the PIVision SQL database...");
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
