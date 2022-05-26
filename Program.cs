using OSIsoft.AF;
using OSIsoft.AF.Asset;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace PIVisionURLAttributeIdentifier
{
    class Program
    {
        static StreamWriter file = new StreamWriter("PIVisionAttributeList_output.txt", append: false);

        static void Main(string[] args)
        {
            file.AutoFlush = true;

            SQLdata visiondata = new SQLdata();
            Utilities util = new Utilities();

            Console.WriteLine("This utility scans all PIVision displays and returns all attributes attached to Value symbols, with their label.");
            Console.WriteLine("Label type definition: F=Full (default), A=Asset, P=Partial, D=Description, C=Custom. (*=symbol part of a collection)");
            Console.WriteLine(); //linebreak

            string sqlInstance = visiondata.ValidatingSQLConnection();
            util.WriteInGreen("Connection to the PIVision SQL database successful");
            util.WriteInGray("Retrieving SQL records...");
            DataTable VisionDataTable = visiondata.PullVisionAttributesGUIDlist(sqlInstance);
            util.WriteInGreen("SQL records retrieved");
            util.WriteInGray("Formatting records...");

            //Faster way to retrieve and format dataTable: building a table of displays containing COG and EditorDisplay, then adding more rows per displays with attr config        
            VisionDataTable = visiondata.formatDTandAddRowBasedOnCOGandEditorDisplay(VisionDataTable);
            visiondata.UpdateDTwithValueSymbolConfig(VisionDataTable);

            util.WriteInGreen("Records formatted. DataTable created.");
            util.WriteInGray("Retrieving meta data from AF...");

            util.WriteInBlue("DisplayID "+ VisionDataTable.Rows[0]["DisplayID"] +": "+ VisionDataTable.Rows[0]["Name"]);
                file.WriteLine("DisplayID " + VisionDataTable.Rows[0]["DisplayID"] + ": " + VisionDataTable.Rows[0]["Name"]); // writing to the output file
                PrintOnlyURLBuilderDRAttr2(VisionDataTable, 0);

                for (int i = 1; i < VisionDataTable.Rows.Count; i++)
                {
                    if (VisionDataTable.Rows[i][1].ToString() != VisionDataTable.Rows[i - 1][1].ToString())
                    {
                        Console.WriteLine(); //linebreak
                        util.WriteInBlue("DisplayID " + VisionDataTable.Rows[i]["DisplayID"] + ": " + VisionDataTable.Rows[i]["Name"]);

                        file.WriteLine(); // writing a line break to the output file
                        file.WriteLine("DisplayID " + VisionDataTable.Rows[i]["DisplayID"] + ": " + VisionDataTable.Rows[i]["Name"]); // writing to the output file
                    }
                    PrintOnlyURLBuilderDRAttr2(VisionDataTable, i);
                }

            util.WriteInGreen("Output has been saved under: PIVision_Label_IdentifierList_output.txt");
            util.PressEnterToExit();
        }

        static void PrintOnlyURLBuilderDRAttr2(DataTable VisionDataTable, int i)
        {
            Utilities util = new Utilities();
            VisionAttribute vizAttribut = new VisionAttribute();
            PISystems myPISystems = new PISystems();

            try
            {
                PISystem myPISystem = myPISystems[VisionDataTable.Rows[i]["Server"].ToString()];
                AFDatabase myDB = myPISystem.Databases[VisionDataTable.Rows[i]["AFDatabase"].ToString()];
                string attributePath = VisionDataTable.Rows[i]["AttributePath"].ToString();

                AFAttribute afAtt = vizAttribut.SearchAndPrint3(attributePath, myDB);
                if (afAtt != null)
                {
                            string label="";
                    var test = VisionDataTable.Rows[i]["LabelType"];
                            switch (VisionDataTable.Rows[i]["LabelType"].ToString())
                            {
                                case "F":                                   
                                    label = afAtt.GetPath().Substring(afAtt.Database.GetPath().Length).Substring(1);
                                    break;
                                case "P":
                                    label = VisionDataTable.Rows[i]["AFattributeName"].ToString();
                                    break;
                                case "D":
                                    label = afAtt.Description;
                                    break;
                                case "A":
                                    label = afAtt.Element.Name;
                                    break;
                                case "C":
                                    label = VisionDataTable.Rows[i]["CustomLabel"].ToString();
                                    break;
                                case "F*":                                   
                                    label = afAtt.GetPath().Substring(afAtt.Database.GetPath().Length).Substring(1);
                                    break;
                                case "P*":
                                    label = VisionDataTable.Rows[i]["AFattributeName"].ToString().Substring(1);
                                    break;
                                case "D*":
                                    label = afAtt.Description;
                                    break;
                                case "A*":
                                    label = afAtt.Element.Name;
                                    break;
                                case "C*":
                                    label = VisionDataTable.Rows[i]["CustomLabel"].ToString();
                                    break;
                    }

                    string datareference = (afAtt.DataReferencePlugIn != null) ? afAtt.DataReferencePlugIn.ToString() : "None";

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("{0,-55}{2,-20}{3,-20}{4,-16}{5,-70}{6,5}",
                                "Attr: " + afAtt.Name
                                , " | DR: " + datareference
                                , " | ShowLabel: " + VisionDataTable.Rows[i]["ShowLabel"]
                                , " | ShowValue: " + VisionDataTable.Rows[i]["ShowValue"]
                                , " | LabelType: " + VisionDataTable.Rows[i]["LabelType"]
                                , " | Label: " + label
                                , " | Value: " + afAtt.GetValue()
                                ) ;
                    Console.ForegroundColor = ConsoleColor.White;
                    file.WriteLine("Attr_name: " + afAtt.Name + 
                        ", DR: " + datareference + 
                        ", ShowLabel: " + VisionDataTable.Rows[i]["ShowLabel"]  + 
                        ", ShowValue: " + VisionDataTable.Rows[i]["ShowValue"] + 
                        ", LabelType: " + VisionDataTable.Rows[i]["LabelType"] + 
                        ", LabelValue: " + label + 
                        " , path: " + afAtt.GetPath() + 
                        ", Attr_Value: " + afAtt.GetValue() + 
                        ", Description: " + afAtt.Description); // writing to the output file
                        
                }
                else
                {
                    util.WriteInRed("attribute " + attributePath + " not found");
                    file.WriteLine("attribute " + attributePath + " not found"); // writing to the output file
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "Object reference not set to an instance of an object.")
                {
                    util.WriteInRed("Could not connect to AF dB: " + VisionDataTable.Rows[i]["AFDatabase"].ToString());
                    file.WriteLine("Could not connect to AF dB: " + VisionDataTable.Rows[i]["AFDatabase"].ToString());
                }
                else
                {
                    util.WriteInRed(ex.Message);
                    file.WriteLine(ex.Message);
                }
                
            }

        }
        static async Task WriteFileLine(StreamWriter streamWriter, string text)
        {
            await streamWriter.WriteLineAsync(text);
            await streamWriter.FlushAsync();
        }
    }

}
