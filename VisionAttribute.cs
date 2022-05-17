using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSIsoft.AF.Asset;
using OSIsoft.AF;
using OSIsoft.AF.Search;

namespace PIVisionURLAttributeIdentifier
{
    class VisionAttribute
    {

        public AFAttribute SearchAndPrint2(PISystem afserver, Guid ElementGUID, Guid AttributeGUID)
        {
            try
            {
                AFElement afelement = AFElement.FindElement(afserver, ElementGUID);
                AFAttribute afattribute = AFAttribute.FindAttribute(afelement, AttributeGUID);
                return afattribute;
            }
            catch
            {
                return null;
            }
        }

        public AFAttribute SearchAndPrint3(string path, AFDatabase myDB)
        {
            try
            {

                AFAttribute afattribute = AFAttribute.FindAttribute(path, myDB);
                return afattribute;
            }
            catch
            {
                return null;
            }

        }
    }
}
