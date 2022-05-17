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

            string sqlInstance = visiondata.ValidatingSQLConnection();
            DataTable VisionDataTable = visiondata.PullVisionAttributesGUIDlist(sqlInstance);
            VisionDataTable = visiondata.FormatDatable_create_attribute_element_columns(VisionDataTable);

            /*            foreach(DataRow dr in VisionDataTable.Rows)
                        {
                            Console.WriteLine("attribute name: " + dr["AFattributeName"] + ", GUID: " + dr["AFattributeGUID"]);
                        }*/
            visiondata.FormatDatatable_getSymbolconfig(VisionDataTable);

            bool confirm = util.Confirm("List only value symbol URI Builder DR attributes? If not, it will list all DR attributes attached to value symbols");
            if (confirm)
            {
                util.WriteInBlue("Display: " + VisionDataTable.Rows[0][1]);
                file.WriteLine("Display: " + VisionDataTable.Rows[0][1]); // writing to the output file
                PrintOnlyURLBuilderDRAttr2(VisionDataTable, 0);

                for (int i = 1; i < VisionDataTable.Rows.Count; i++)
                {
                    if (VisionDataTable.Rows[i][1].ToString() != VisionDataTable.Rows[i - 1][1].ToString())
                    {
                        Console.WriteLine(); //linebreak
                        util.WriteInBlue("Display: " + VisionDataTable.Rows[i][1]);

                        file.WriteLine(); // writing a line break to the output file
                        file.WriteLine("Display: " + VisionDataTable.Rows[i][1]); // writing to the output file
                    }
                    PrintOnlyURLBuilderDRAttr2(VisionDataTable, i);
                }
            }

            else
            {
                util.WriteInBlue("Display: " + VisionDataTable.Rows[0][1]);

                file.WriteLine("Display: " + VisionDataTable.Rows[0][1]); // writing to the output file

                PrintAttributeDetail2(VisionDataTable, 0);

                for (int i = 1; i < VisionDataTable.Rows.Count; i++)
                {


                    if (VisionDataTable.Rows[i][1].ToString() != VisionDataTable.Rows[i - 1][1].ToString())
                    {
                        Console.WriteLine(); //linebreak
                        util.WriteInBlue("Display: " + VisionDataTable.Rows[i][1]);

                        file.WriteLine(); // writing a line break to the output file
                        file.WriteLine("Display: " + VisionDataTable.Rows[i][1]); // writing to the output file
                    }

                    PrintAttributeDetail2(VisionDataTable, i);
                }
            }
            util.WriteInGreen("Output has been saved under: PIVisionAttributeList_output.txt");
            util.PressEnterToExit();
        }

        static void PrintAttributeDetail(DataTable VisionDataTable, int i)
        {
            Utilities util = new Utilities();
            VisionAttribute vizAttribut = new VisionAttribute();
            PISystems myPISystems0 = new PISystems();
            PISystem myPISystem0 = myPISystems0[VisionDataTable.Rows[i][2].ToString()];
            Guid eltGUID0 = new Guid(VisionDataTable.Rows[i][3].ToString());
            Guid AttGUID0 = new Guid(VisionDataTable.Rows[i][4].ToString());

            AFAttribute afAtt0 = vizAttribut.SearchAndPrint2(myPISystem0, eltGUID0, AttGUID0);
            if (afAtt0 != null)
            {
                util.WriteInYellow("Name: " + afAtt0.Name + " | DR: " + afAtt0.DataReferencePlugIn + " | path: " + afAtt0.GetPath());
            }
            else
            {
                util.WriteInRed("no read access on " + myPISystem0);
            }
        }

        static void PrintOnlyAnalysisDRAttr(DataTable VisionDataTable, int i)
        {
            Utilities util = new Utilities();
            VisionAttribute vizAttribut = new VisionAttribute();
            PISystems myPISystems = new PISystems();
            PISystem myPISystem = myPISystems[VisionDataTable.Rows[i][2].ToString()];
            Guid eltGUID = new Guid(VisionDataTable.Rows[i][3].ToString());
            Guid AttGUID = new Guid(VisionDataTable.Rows[i][4].ToString());

            AFAttribute afAtt = vizAttribut.SearchAndPrint2(myPISystem, eltGUID, AttGUID);

            if (afAtt != null)
            {
                if (afAtt.DataReferencePlugIn != null)
                    if (afAtt.DataReferencePlugIn.ToString() == "Analysis")
                    {
                        util.WriteInYellow("Name: " + afAtt.Name + " | DR: " + afAtt.DataReferencePlugIn + " | path: " + afAtt.GetPath());
                    }
            }
            else
            {
                util.WriteInRed("no read access on " + myPISystem);
            }

        }

        static void PrintAttributeDetail2(DataTable VisionDataTable, int i)
        {
            Utilities util = new Utilities();
            VisionAttribute vizAttribut = new VisionAttribute();
            PISystems myPISystems = new PISystems();

            try
            {
                PISystem myPISystem = myPISystems[VisionDataTable.Rows[i][2].ToString()];
                AFDatabase myDB = myPISystem.Databases[VisionDataTable.Rows[i]["AFDatabase"].ToString()];
                string attributePath = VisionDataTable.Rows[i]["AttributePath"].ToString();

                AFAttribute afAtt = vizAttribut.SearchAndPrint3(attributePath, myDB);
                if (afAtt != null)
                {
                    util.WriteInYellow("Attr_name: " + afAtt.Name + " | DR: " + afAtt.DataReferencePlugIn + " | path: " + afAtt.GetPath() + " | Value: " + afAtt.GetValue() + " | Description: " + afAtt.Description + " | LabelType: " + VisionDataTable.Rows[i]["LabelType"]);

                    file.WriteLine("Attr_name: " + afAtt.Name + " | DR: " + afAtt.DataReferencePlugIn + " | symbol#: " + VisionDataTable.Rows[i]["SymbolNumber"] + " | path: " + afAtt.GetPath() + " | Value: " + afAtt.GetValue() + " | Description: " + afAtt.Description + " | LabelType: " + VisionDataTable.Rows[i]["LabelType"]); // writing to the output file
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

        static void PrintOnlyURLBuilderDRAttr2(DataTable VisionDataTable, int i)
        {
            Utilities util = new Utilities();
            VisionAttribute vizAttribut = new VisionAttribute();
            PISystems myPISystems = new PISystems();

            try
            {
                PISystem myPISystem = myPISystems[VisionDataTable.Rows[i][2].ToString()];
                AFDatabase myDB = myPISystem.Databases[VisionDataTable.Rows[i]["AFDatabase"].ToString()];
                string attributePath = VisionDataTable.Rows[i]["AttributePath"].ToString();

                AFAttribute afAtt = vizAttribut.SearchAndPrint3(attributePath, myDB);
                if (afAtt != null)
                {
                    if (afAtt.DataReferencePlugIn != null)
                        if (afAtt.DataReferencePlugIn.ToString() == "URI Builder")
                        {
                            util.WriteInYellow("Attr_name: " + afAtt.Name + " | DR: " + afAtt.DataReferencePlugIn + " | path: " + afAtt.GetPath() + " | Value: "+afAtt.GetValue()+ " | Description: "+afAtt.Description + " | LabelType: " + VisionDataTable.Rows[i]["LabelType"]);

                            file.WriteLine("Attr_name: " + afAtt.Name + " | DR: " + afAtt.DataReferencePlugIn + " | symbol#: " + VisionDataTable.Rows[i]["SymbolNumber"] + " | path: " + afAtt.GetPath() + " | Value: " + afAtt.GetValue() + " | Description: " + afAtt.Description + " | LabelType: " + VisionDataTable.Rows[i]["LabelType"]); // writing to the output file
                        }
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
