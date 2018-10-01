using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using WebApiProxy.Typescript.Core;
using WebApiProxy.TypeScript.Core.Models;

namespace WebApiProxy.TypeScript.Core
{
    public class MetaDataLocalProvider
    {
        private readonly List<ModelDefinition> models;
        private readonly List<string> typesToIgnore = new List<string>();
        //private HttpConfiguration config;

        public MetaDataLocalProvider()
        {
            //config = new HttpConfiguration();
            //config.Configure(WebApiConfig.Register);


            // Start OWIN host 

            this.models = new List<ModelDefinition>();
            this.typesToIgnore = new List<string>();
            //this.config = GlobalConfiguration.Configuration;
        }

        private List<string> GetUrlParameters(string baseUrl)
        {
            var lst = new List<string>();

            var sp = baseUrl.Split('{');
            for (int i=1; i < sp.Length; i++)
            {
                var s = sp[i];
                var sp2 = s.Split('}');
                lst.Add(sp2[0]);
            }



            return lst;
        }

        //private static string RouteName(RouteBase original)
        //{
        //    var routes = System.Web.Routing.RouteTable.Routes;

        //    if (routes.Contains(original))
        //    {
        //        var namedMapField = routes.GetType().GetField("_namedMap", BindingFlags.NonPublic | BindingFlags.Instance);
        //        var namedMap = namedMapField.GetValue(routes) as Dictionary<string, RouteBase>;

        //        var query =
        //            from pair in namedMap
        //            where pair.Value == original
        //            select pair.Key;

        //        return query.Single();
        //    }

        //    return string.Empty;
        //}
        public Metadata GetMetadata()
        {
            var host = "";
            
            var descriptions = config.Services.GetApiExplorer().ApiDescriptions;
            var documentationProvider = config.Services.GetDocumentationProvider();

            ILookup<HttpControllerDescriptor, ApiDescription> apiGroups = descriptions
                .Where(a => !a.ActionDescriptor.ControllerDescriptor.ControllerType.IsAbstract
                            && !a.RelativePath.Contains("Swagger")
                            && !a.RelativePath.Contains("docs"))
                .ToLookup(a => a.ActionDescriptor.ControllerDescriptor);


            var defs = (from d in apiGroups
                        where !d.Key.ControllerType.IsExcluded()
                        select new ControllerDefinition
                        {
                            Name = d.Key.ControllerName,
                            Description = documentationProvider == null
                                ? ""
                                : documentationProvider.GetDocumentation(d.Key) ?? "",
                            ActionMethods = (from a in descriptions
                                             where !a.ActionDescriptor.ControllerDescriptor.ControllerType.IsExcluded()
                                                   && !a.ActionDescriptor.IsExcluded()
                                                   && !a.RelativePath.Contains("Swagger")
                                                   && !a.RelativePath.Contains("docs")
                                                   && a.ActionDescriptor.ControllerDescriptor.ControllerName == d.Key.ControllerName
                                             select new ActionMethodDefinition
                                             {
                                                 Name = a.ActionDescriptor.ActionName,
                                                 BodyParameter = (from b in a.ParameterDescriptions
                                                                  where b.Source == ApiParameterSource.FromBody
                                                                  select new ParameterDefinition
                                                                  {
                                                                      Name = b.ParameterDescriptor.ParameterName,
                                                                      Type = ParseType(b.ParameterDescriptor.ParameterType),
                                                                      Description = b.Documentation ?? "",
                                                                      IsOptional = b.ParameterDescriptor.IsOptional,
                                                                      DefaultValue = b.ParameterDescriptor.DefaultValue
                                                                  }).FirstOrDefault(),
                                                 UrlParameters =
                                                 (from b in a.ParameterDescriptions.Where(p => p.ParameterDescriptor != null)
                                                  where b.Source == ApiParameterSource.FromUri
                                                  select new ParameterDefinition
                                                  {
                                                      Name = b.ParameterDescriptor.ParameterName,
                                                      Type = ParseType(b.ParameterDescriptor.ParameterType),
                                                      Description = b.Documentation ?? "",
                                                      IsOptional = b.ParameterDescriptor.IsOptional,
                                                      DefaultValue = b.ParameterDescriptor.DefaultValue
                                                  }).ToList(),
                                                 Url = a.RelativePath,

                                                 Description = a.Documentation ?? "",
                                                 ReturnType = ParseType(a.ResponseDescription.ResponseType ??
                                                                        a.ResponseDescription.DeclaredType),
                                                 Type = a.HttpMethod.Method
                                             }).ToList()
                        }).ToList();




            var asms = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var controllers = (from asm in asms
                               from type in asm.GetTypes()
                               where typeof(Controller).IsAssignableFrom(type)
                               select type).ToList();

            var lst = (from c in controllers
                       select new ControllerDefinition
                       {
                           Name = c.Name.Replace("Controller",""),
                           Description = c.Namespace,
                           ActionMethods = (from method in c.GetMethods()
                                            where method.IsPublic &&
                                            !method.IsDefined(typeof(NonActionAttribute)) &&
                                            method.IsDefined(typeof(ExportToApiAttribute))
                                            select new ActionMethodDefinition()
                                            {
                                                Name = method.Name,
                                                BodyParameter = null,
                                                UrlParameters =
                                                (from b in method.GetParameters()
                                                 select new ParameterDefinition
                                                 {
                                                     Name = b.Name,
                                                     Type = ParseType(b.ParameterType),
                                                     Description = "",
                                                     IsOptional = b.IsOptional,
                                                     DefaultValue = b.DefaultValue
                                                 }).ToList(),
                                                Url = "/",
                                                Description = method.GetCustomAttribute<ExportToApiAttribute>().RouteToUse,
                                                ReturnType = "void",
                                                Type = "get"

                                            })
                       });

            lst = (from c in lst where c.ActionMethods.Any() select c).ToList();


            HttpContext httpContext = HttpContext.Current;

            if (httpContext == null)
            {
                HttpRequest request = new HttpRequest("/", "http://" + Environment.MachineName, "");
                HttpResponse response = new HttpResponse(new StringWriter());
                httpContext = new HttpContext(request, response);
            }

            HttpContextWrapper httpContextBase = new HttpContextWrapper(httpContext);
            RouteData routeData = new RouteData();
            var myHostContext = new RequestContext(httpContextBase, routeData);
            var Url = new UrlHelper(myHostContext);


            foreach (var c in lst)
            {
                var acts = c.ActionMethods.ToList();
                var methods = new List<ActionMethodDefinition>();
                var finalName = c.Name;
                foreach (var act in acts)
                {
                    var isInArea = false;
                    foreach (var rb in Url.RouteCollection)
                    {
                        try
                        {
                            var currentHasArea = false;
                            var valid = true;
                            var rvd = new RouteValueDictionary();
                            var r = rb as Route;

                            if (r.DataTokens.ContainsKey("area"))
                            {
                                currentHasArea = true;
                                var area = r.DataTokens["area"];


                                valid = c.Description.ToUpper().Contains("." + area.ToString().ToUpper() + ".");
                                if (valid)
                                {
                                    finalName = area + "_" + c.Name;
                                    isInArea = true;
                                }

                            }

                            var rName = RouteName(rb);

                            if (!string.IsNullOrWhiteSpace(act.Description))
                            {
                                if (rName.ToUpper().Trim() != act.Description.ToUpper().Trim())
                                {
                                    valid = false;
                                }
                            }

                            if (valid && (currentHasArea || (!currentHasArea && !isInArea) ))
                            {

                                var pLst = GetUrlParameters(r?.Url);

                                foreach (var p in pLst)
                                {
                                    var name = p.Replace("*", "");
                                    rvd[name] = "[" + name + "]";
                                }

                                rvd["controller"] = c.Name;
                                rvd["action"] = act.Name;
                                foreach (var m in act.UrlParameters)
                                {
                                    var name = m.Name.Replace("*", "");
                                    rvd[name] = "[" + name + "]";
                                }

                                var act2 = new ActionMethodDefinition()
                                {
                                    Name = act.Name,
                                    BodyParameter = act.BodyParameter,
                                    Description = "",
                                    ReturnType = act.ReturnType,
                                    Type = act.Type,
                                    Url = "",
                                    UrlParameters = act.UrlParameters.ToList()
                                };


                                act2.Url = Url.RouteUrl(rName, rvd);
                                act2.Url = act2.Url?.Replace("[", "{").Replace("]", "}").Replace("%5B", "{")
                                    .Replace("%5D", "}");

                                if (!string.IsNullOrWhiteSpace(act2.Url))
                                {
                                    methods.Add(act2);
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);

                        }
                    }





                }


                c.ActionMethods = methods;
                c.Name = finalName;
            }




            defs.AddRange(lst);


            var metadata = new Metadata
            {
                Definitions = defs,
                Models = models,
                Host = (null != host && host.Length > 0 && host[host.Length - 1] != '/')
                    ? string.Concat(host, "/")
                    : host
            };

            metadata.Definitions = metadata.Definitions.Distinct().OrderBy(d => d.Name);
            metadata.Models = metadata.Models.Distinct(new ModelDefinitionEqualityComparer()).OrderBy(d => d.Name);
            return metadata;


        }

        private string ParseType(Type type, ModelDefinition model = null)
        {
            string res;

            if (type == null)
                return "";

            // If the type is a generic type format to correct class name.
            if (type.IsGenericType)
            {
                res = GetGenericRepresentation(type, (t) => ParseType(t, model), model);

                AddModelDefinition(type);
            }
            else
            {
                if (!type.IsEnum && type.ToString().StartsWith("System."))
                {
                    if (type.ToString().Equals("System.Void"))
                        res = "void";
                    else
                        res = type.Name;
                }
                else
                {
                    res = type.Name;

                    if (!type.IsGenericParameter)
                    {
                        AddModelDefinition(type);
                    }
                }
            }

            return res;
        }

        private string GetGenericRepresentation(Type type, Func<Type, string> getTypedParameterRepresentation, ModelDefinition model = null)
        {
            string res = type.Name;
            int index = res.IndexOf('`');
            if (index > -1)
                res = res.Substring(0, index);

            Type[] args = type.GetGenericArguments();

            res += "<";

            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    res += ", ";
                //Recursivly find nested arguments

                var arg = args[i];
                if (model != null && model.IsGenericArgument(arg.Name))
                {
                    res += model.GetGenericArgument(arg.Name);
                }
                else
                {
                    res += getTypedParameterRepresentation(arg);
                }
            }
            res += ">";
            return res;
        }

        private string GetGenericTypeDefineRepresentation(Type genericTypeDefClass)
        {

            string res = genericTypeDefClass.Name;
            int index = res.IndexOf('`');
            if (index > -1)
                res = res.Substring(0, index);

            Type[] args = genericTypeDefClass.GetGenericArguments();

            res += "<";

            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    res += ", ";

                var arg = args[i];
                res += arg.Name;
            }

            res += ">";
            return res;
        }



        private void AddModelDefinition(Type classToDef)
        {
            //var documentationProvider = config.Services.GetDocumentationProvider();
            //When the class is an array redefine the classToDef as the array type
            if (classToDef.IsArray)
            {
                classToDef = classToDef.GetElementType();
            }
            // Is is not a .NET Framework generic, then add to the models collection.
            if (!classToDef.IsEnum && classToDef.Namespace.StartsWith("System", StringComparison.OrdinalIgnoreCase))
            {
                AddTypeToIgnore(classToDef.Name);
                return;
            }
            //If the class has not been mapped then map into metadata
            if (!typesToIgnore.Contains(classToDef.Name))
            {
                ModelDefinition model = new ModelDefinition();
                model.Name = classToDef.Name;
                model.Description = "";//GetDescription(classToDef);
                if (classToDef.IsGenericType)
                {
                    model.Name = GetGenericTypeDefineRepresentation(classToDef.GetGenericTypeDefinition());
                }
                model.Type = classToDef.IsEnum ? "enum" : "class";

                if (!classToDef.IsEnum)
                {
                    var constants = classToDef
                        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                        .Where(f => f.IsLiteral && !f.IsInitOnly)
                        .ToList();
                    model.Constants = from constant in constants
                                      select new ConstantDefinition
                                      {
                                          Name = constant.Name,
                                          Type = ParseType(constant.FieldType),
                                          Value = GetConstantValue(constant),
                                          Description = ""//GetDescription(constant)
                                      };

                    var properties = classToDef.IsGenericType
                        ? classToDef.GetGenericTypeDefinition().GetProperties()
                        : classToDef.GetProperties();

                    model.Properties = from property in properties
                                       select new ModelProperty
                                       {
                                           Name = property.Name,
                                           Type = ParseType(property.PropertyType, model),
                                           Description = ""//GetDescription(property)
                                       };

                    AddTypeToIgnore(model.Name);
                    foreach (var p in properties)
                    {
                        var type = p.PropertyType;

                        if (!models.Any(c => c.Name.Equals(type.Name)))// && !type.IsInterface)
                        {
                            ParseType(type);
                        }
                    }
                }
                else
                {

                    var enumValues = Enum.GetValues(classToDef);
                    var enumNames = Enum.GetNames(classToDef);
                    var properties = new List<ModelProperty>();
                    model.Properties = properties;

                    for (int a = 0; a < enumValues.Length; a++)
                    {
                        properties.Add(new ModelProperty
                        {
                            Name = enumNames[a],
                            Description = ((int)enumValues.GetValue(a)).ToString()
                        });
                    }


                    AddTypeToIgnore(model.Name);
                }

                models.Add(model);
            }
        }

        private void AddTypeToIgnore(string name)
        {
            if (!typesToIgnore.Contains(name))
            {
                typesToIgnore.Add(name);
            }
        }

        private string GetConstantValue(FieldInfo constant)
        {
            var value = constant.GetRawConstantValue().ToString();
            return value;
        }

        //private static string GetDescription(MemberInfo member)
        //{
        //    var xml = DocsService.GetXmlFromMember(member, false);
        //    if (xml != null)
        //    {
        //        return xml.InnerText.Trim();
        //    }
        //    return String.Empty;
        //}

        //private static string GetDescription(Type type)
        //{
        //    var xml = DocsService.GetXmlFromType(type, false);

        //    if (xml != null)
        //    {
        //        return xml.InnerText.Trim();
        //    }
        //    return String.Empty;
        //}
    }
}