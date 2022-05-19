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
            /* file.WriteLine("test");*/

            SQLdata visiondata = new SQLdata();
            Utilities util = new Utilities();

            Console.WriteLine("This utility scans all PIVision displays and returns all attributes attached to Value symbols, with their label.");
            Console.WriteLine("Attribute is assumed to be used only once per display. Only attributes with GUIDs in COG are returned");
            Console.WriteLine("Label type definition: F=Full (default), A=Asset, P=Partial, D=Description, C=Custom");
            Console.WriteLine(); //linebreak

            string sqlInstance = visiondata.ValidatingSQLConnection();
            util.WriteInGreen("Connection to the PIVision SQL database successful");
            util.WriteInYellow("Retrieving SQL records...");
            DataTable VisionDataTable = visiondata.PullVisionAttributesGUIDlist(sqlInstance);
            util.WriteInGreen("SQL records retrieved");
            util.WriteInYellow("Formatting records...");
            VisionDataTable = visiondata.FormatDatable_create_attribute_element_columns(VisionDataTable);
            visiondata.FormatDatatable_getSymbolconfig(VisionDataTable);

                util.WriteInBlue("DisplayID "+ VisionDataTable.Rows[0]["DisplayID"] +": "+ VisionDataTable.Rows[0]["Name"]);
                file.WriteLine("Display: " + VisionDataTable.Rows[0][1]); // writing to the output file
                PrintOnlyURLBuilderDRAttr2(VisionDataTable, 0);

                for (int i = 1; i < VisionDataTable.Rows.Count; i++)
                {
                    if (VisionDataTable.Rows[i][1].ToString() != VisionDataTable.Rows[i - 1][1].ToString())
                    {
                        Console.WriteLine(); //linebreak
                        util.WriteInBlue("DisplayID " + VisionDataTable.Rows[i]["DisplayID"] + ": " + VisionDataTable.Rows[i]["Name"]);

                        file.WriteLine(); // writing a line break to the output file
                        file.WriteLine("Display: " + VisionDataTable.Rows[i][1]); // writing to the output file
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
                   /* if (afAtt.DataReferencePlugIn != null)
                    {*/

                    
                       /* if (afAtt.DataReferencePlugIn.ToString() == "String Builder" || afAtt.DataReferencePlugIn.ToString() == "" || afAtt.DataReferencePlugIn.ToString() == "URI Builder")
                        {*/
                            /*string label = (VisionDataTable.Rows[i]["LabelType"].ToString() == "P")? VisionDataTable.Rows[i]["AFattributeName"].ToString().Substring(1) : "f";*/
                            string label="";

                            switch (VisionDataTable.Rows[i]["LabelType"].ToString())
                            {
                                case "F":                                   
                                    label = afAtt.GetPath().Substring(afAtt.Database.GetPath().Length).Substring(1);
                                    break;
                                case "P":
                                    label = VisionDataTable.Rows[i]["AFattributeName"].ToString().Substring(1);
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
                                case "F (collection)":                                   
                                    label = afAtt.GetPath().Substring(afAtt.Database.GetPath().Length).Substring(1);
                                    break;
                                case "P (collection)":
                                    label = VisionDataTable.Rows[i]["AFattributeName"].ToString().Substring(1);
                                    break;
                                case "D (collection)":
                                    label = afAtt.Description;
                                    break;
                                case "A (collection)":
                                    label = afAtt.Element.Name;
                                    break;
                                case "C (collection)":
                                    label = VisionDataTable.Rows[i]["CustomLabel"].ToString();
                                    break;
                    }

                    string datareference = (afAtt.DataReferencePlugIn != null) ? afAtt.DataReferencePlugIn.ToString() : "None";

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("{0,-40}{1,-23}{2,-28}{3,-40}",
                                "Attr_name: " + afAtt.Name 
                                , " | DR: " + datareference 
                                , " | LabelType: " + VisionDataTable.Rows[i]["LabelType"]
                                ," | Label: "+label );
                    Console.ForegroundColor = ConsoleColor.White;
                    file.WriteLine("Attr_name: " + afAtt.Name + " , DR: " + datareference + " , symbol#: " + VisionDataTable.Rows[i]["SymbolNumber"]  + " , LabelType: " + VisionDataTable.Rows[i]["LabelType"] + " , Label: " + label + " , path: " + afAtt.GetPath() + " , Value: " + afAtt.GetValue() + " , Description: " + afAtt.Description); // writing to the output file
                        /* }*/
                    /*}*/
                }
                else
                {
                    util.WriteInRed("attribute " + attributePath + " not found");
                    file.WriteLine("attribute " + attributePath + " not found"); // writing to the output file
                }
            }
            catch (Exception ex)
            {
                util.WriteInRed(ex.Message);
            }

        }
        static async Task WriteFileLine(StreamWriter streamWriter, string text)
        {
            await streamWriter.WriteLineAsync(text);
            await streamWriter.FlushAsync();
        }
    }

}
