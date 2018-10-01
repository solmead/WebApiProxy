using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using WebApiProxy.Core.Models;

namespace WebApiProxy.TypeScript
{
    public static class Extensions
    {

        public static List<ActionMethodDefinition> GetMethods(IEnumerable<ActionMethodDefinition> methods)
        {
            var names = (from m in methods select m.Name).Distinct().ToList();
            var methodList = new List<ActionMethodDefinition>();
            foreach (var n in names)
            {
                var mList = (from m in methods where m.Name == n select new
                {
                    Key = m.Name + "_" + m.UrlParameters.Count() + "_" + (m.BodyParameter != null ? "Y" : "N"),
                    m
                }).ToList();

                var subNames = (from m in mList select m.Key).Distinct().ToList();
                var cnt = 1;
                foreach (var sn in subNames)
                {
                    var item = (from m in mList where m.Key == sn select m.m).First();
                    if (cnt > 1)
                    {
                        item.Name = item.Name + cnt;
                    }
                    methodList.Add(item);
                    cnt++;
                }


                //var subNames = (from m in mList select  )
                

            }



            //

            return methodList;
        }


        public static string Default(string type)
        {

            if (type.Contains("Nullable"))
            {
                return "null";
            }
            var numbers = new List<string>
            {
                "Int16",
                "Int32",
                "Int64",
                "UInt16",
                "UInt32",
                "UInt64",
                "Single",
                "Double",
                "Decimal",
                "Byte",
                "SByte"
            };
            foreach (var s in numbers)
            {
                if (type.Contains(s))
                {
                    return "0";
                }
            }

            if (type.Contains("Boolean"))
            {
                return "false";
            }

            if (type.Contains("DateTime"))
            {
                return "new Date()";
            }
            

            return "null";
        }
        public static bool IsValid(string type)
        {
            if (type.Contains("Func"))
            {
                return false;
            }
            return true;
        }
        public static bool IsNullable(string type)
        {
            return type.Contains("Nullable");
        }

        public static ModelDefinition GetModel(string type, Metadata metadata)
        {
            if (type.Contains("<"))
            {
                type = type.Substring(type.IndexOf('<') + 1);
                type = type.Substring(0, type.IndexOf('>'));
            }


            var model = (from m in metadata.Models where type == m.Name select m).FirstOrDefault();
            if (model == null)
            {
                model = new ModelDefinition()
                {
                    Name = type,
                    Properties = new List<ModelProperty>()
                };
            }

            return model;
        }


        public static bool IsBaseType(string type)
        {

            if (string.IsNullOrWhiteSpace(type))
            {
                return true;
            }

            if (type.Contains("Nullable"))
            {
                return true;
            }

            var numbers = new List<string>
            {
                "Int16",
                "Int32",
                "Int64",
                "UInt16",
                "UInt32",
                "UInt64",
                "Single",
                "Double",
                "Decimal",
                "Byte",
                "SByte"
            };
            foreach (var s in numbers)
            {
                if (type.Contains(s))
                {
                    return true;
                }
            }

            if (type.Contains("Boolean"))
            {
                return true;
            }

            if (type.Contains("DateTime"))
            {
                return true;
            }
            if (type.Contains("TimeSpan"))
            {
                return true;
            }
            if (type.Contains("Object"))
            {
                return true;
            }
            if (type.Contains("FileInfo"))
            {
                return true;
            }

            if (type.Contains("String"))
            {
                return true;
            }

            if (type.Contains("Char"))
            {
                return true;
            }


            return false;
        }
        public static string HandleType(string basetype)
        {
            var type = basetype;
            if (string.IsNullOrWhiteSpace(type))
            {
                type = "void";
            }
            if (type.Contains("Nullable"))
            {
                type = type.Replace("Nullable<", "").Replace(">", "");// + "?";
            }

            var numbers = new List<string>
            {
                "Int16",
                "Int32",
                "Int64",
                "UInt16",
                "UInt32",
                "UInt64",
                "Single",
                "Double",
                "Decimal",
                "Byte",
                "SByte"
            };
            numbers.ForEach((s) =>
            {
                type = type.Replace(s, "number");
            });
            type = type.Replace("Boolean", "boolean");
            //DateTime
            //type = type.Replace("DateTime", "string");
            type = type.Replace("DateTime", "Date");
            type = type.Replace("TimeSpan", "string");

            type = type.Replace("Object", "any");
            type = type.Replace("FileInfo", "any");

            type = type.Replace("String", "string");
            type = type.Replace("Char", "string");


            type = type.Replace("List<", "Array<");
            type = type.Replace("IList", "Array");
            type = type.Replace("IEnumerable", "Array");
            type = type.Replace("ICollection", "Array");
            type = type.Replace("IQueryable", "Array");
            type = type.Replace("IArray", "Array");



            return type;
        }

    }
}
