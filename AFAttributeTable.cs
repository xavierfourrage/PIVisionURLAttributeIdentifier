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
     
    class AFAttributeTable
    {
        public DataTable createDT()
        {
            DataTable attributeTable = new DataTable();
            attributeTable.Columns.Add("DisplayID", typeof(string));
            attributeTable.Columns.Add("Name", typeof(string));
            attributeTable.Columns.Add("Server", typeof(string));
/*            attributeTable.Columns.Add("FullDatasource", typeof(string));*/
            attributeTable.Columns.Add("EditorDisplay", typeof(string));
            attributeTable.Columns.Add("COG", typeof(string));
/*            attributeTable.Columns.Add("Collection", typeof(string));*/
            attributeTable.Columns.Add("AFDatabase", typeof(string));
            attributeTable.Columns.Add("AttributePath", typeof(string));
            attributeTable.Columns.Add("AFattributeName", typeof(string));
            attributeTable.Columns.Add("AFattributeGUID", typeof(string));
            attributeTable.Columns.Add("SymbolNum", typeof(string));
            attributeTable.Columns.Add("LabelType", typeof(string));
            attributeTable.Columns.Add("CustomLabel", typeof(string));
            attributeTable.Columns.Add("ShowValue", typeof(string));
            attributeTable.Columns.Add("ShowLabel", typeof(string));
            return attributeTable;

        }

    }

}
