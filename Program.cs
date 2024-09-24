using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ConsoleApp1
{
    public class SearchParams
    {
        public string PropertyName { get; set; }
        public string Value { get; set; }
        public string Operator { get; set; }
    }

    public enum TaskStatus
    {  
        Open=0,
        Completed=1 
    }

    public enum TaskAction
    {
        Create= 0,
        Update = 1,
        Query=2
    }

    /// <summary>
    /// this is class to create record for task in SFDC
    /// </summary>
    public class SFDCTask
    {
        public string subject { get; set; }
        public DateTime ActivityDate { get; set; }
        public string Status { get; set; }
        public string Task_Owner_Email__c { get; set; }
        public string Description { get; set; }

        public string IcertisCLM__Instance_Id__c { get; set; }
        public string IcertisCLM__TaskSysId__c { get; set; }
        public string WhatId { get; set; }
    }


    public class CustomizationConstant
    {

        //public const string SFDCSearchQueryTemplate = "SELECT +Id+FROM+task+WHERE+IcertisCLM__TaskSysId__c+=+'{0}'+and+Status+=+'{1}'";
        public const string SFDCSearchQueryTemplate = "SELECT +Id+FROM+task+{0}'";
        public const string EqualOperator = "Equal";
        public const string NotEqualOperator = "Not Equal";
        public const string SubjectTemplateTopicApproval = " Approval topic for {0}";
        public const string SubjectTemplateAgreementPublish = "Created agreement for {0}";
        public const string SubjectTemplateTopicStart = "Colloboration started for {0}";
        public const string StatusField= "Status";

    }

    internal class Program
    {

        //public const string SFDCSearchQueryTemplate = "SELECT +Id+FROM+task+WHERE+IcertisCLM__TaskSysId__c+=+'{0}'+and+IcertisCLM__Instance_Id__c+=+'{1}'";


        private static string GetPayLoadForAgreement(DateTime activityDate,string assignedTo,string subject,string agreementId)
        {
            JArray objResult= new JArray();
            string createdBy = "", whatId="";
            SFDCTask objSFDCTask=new SFDCTask();
            objSFDCTask.subject = subject;
            objSFDCTask.ActivityDate = activityDate;
            objSFDCTask.Status = TaskStatus.Open.ToString();
            objSFDCTask.Task_Owner_Email__c= assignedTo;
            objSFDCTask.Description = "";
            objSFDCTask.IcertisCLM__Instance_Id__c = agreementId;
            objSFDCTask.IcertisCLM__TaskSysId__c = Guid.NewGuid().ToString();
            objSFDCTask.WhatId = whatId;
            string output = JsonConvert.SerializeObject(objSFDCTask);
            return output;
        }


        private static string GetPayLoadForAssociation(DateTime activityDate, string assignedTo, string subject, string associationID)
        {
            JArray objResult = new JArray();
            string createdBy = "", agreementId="", whatId="";
            SFDCTask objSFDCTask = new SFDCTask();
            objSFDCTask.subject = subject;
            objSFDCTask.ActivityDate = activityDate;
            objSFDCTask.Status = TaskStatus.Open.ToString();
            objSFDCTask.Task_Owner_Email__c = assignedTo;
            objSFDCTask.Description = "";
            objSFDCTask.IcertisCLM__Instance_Id__c = agreementId;
            objSFDCTask.IcertisCLM__TaskSysId__c = associationID;
            objSFDCTask.WhatId=whatId;
            string output = JsonConvert.SerializeObject(objSFDCTask);
            return output;
        }

        private static JArray GetTaskListByQuery(List<SearchParams> searchParameters)
        {
            JArray output= new JArray();
            StringBuilder whereQuery = new StringBuilder();
            foreach (SearchParams key in searchParameters)
            {
                if (whereQuery.ToString() == string.Empty)
                {
                    if (key.Operator.Equals(CustomizationConstant.EqualOperator, StringComparison.OrdinalIgnoreCase))
                    {
                        whereQuery.Append($"WHERE+{key.PropertyName}+=+'{key.Value}'");
                    }
                    else if (key.Operator.Equals(CustomizationConstant.NotEqualOperator, StringComparison.OrdinalIgnoreCase))
                    {
                        whereQuery.Append($"WHERE+{key.PropertyName}+<>+'{key.Value}'");
                    }

                }
                else
                {
                    if (key.Operator.Equals(CustomizationConstant.EqualOperator, StringComparison.OrdinalIgnoreCase))
                    {
                        whereQuery.Append($"+and+{key.PropertyName}+=+'{key.Value}'");
                    }
                    else if (key.Operator.Equals(CustomizationConstant.NotEqualOperator, StringComparison.OrdinalIgnoreCase))
                    {
                        whereQuery.Append($"+and+{key.PropertyName}+<>+'{key.Value}'");
                    }
                }
            }
            string query = string.Format(CustomizationConstant.SFDCSearchQueryTemplate, whereQuery.ToString());
            output.Add(PostDataToSFDC("", TaskAction.Query,query,""));  //Alert
            return output;
        }

        /// <summary>
        /// 
        /// </summary>
        private static JObject PostDataToSFDC(string jsonData,TaskAction action,string queryString,string taskID)
        {
            string url = "https://solvnet--commondev1.sandbox.my.salesforce.com/services/data/v61.0/sobjects/Task", bearerToken= "00D7i000000Vj9q!AQkAQAWwqnGayCbbPKRdAnAt_oCDUCamuFgEeh23sN_tv.3m4hTvGUFVQQw6vY6H3Bi6UYAtpIrj.cphGqgcQEp3yFF.IMGL";
            JObject output = new JObject();
            HttpMethod method = null;
            if (action==TaskAction.Create)
            {
                url = url + "/sobjects/Task";
                method = HttpMethod.Post;
            }
            else if (action == TaskAction.Update)
            {
                url = url + $"/sobjects/Task/{taskID}";
                method= HttpMethod.Put;   //Alert

            }
            else if (action == TaskAction.Query)
            {
                url = url + "/query?q="+ queryString;
                method = HttpMethod.Get;
            }
            using (HttpClient client = new HttpClient())
            {

                var request = new HttpRequestMessage(method, url);
                if(jsonData!=string.Empty)
                {
                    request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                };

                // Set the Authorization header with the Bearer token
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var response = client.SendAsync(request).Result;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string responseData = response.Content.ReadAsStringAsync().Result;
                    if (responseData != string.Empty)
                    {
                        output= JObject.Parse(responseData);
                    }
                }
                return output;

            }

        }
        static void Main(string[] args)
        {
            DateTime activityDate = DateTime.UtcNow; 
            string agreementId = "082cde25-2c0c-431c-bf5c-53ccdc5d7b3e", associationId=Guid.NewGuid().ToString(), assignedTo= "test@gmail.com",
                action= "Send For Approval", typeOfContract= "Association", agreementCode="",subject="";

            if(typeOfContract=="Association")
            {
                if(action=="Send For Approval")
                {
                    subject = $"Approval topic for {agreementCode}";
                    string data = GetPayLoadForAssociation(activityDate, assignedTo, subject, associationId);
                    PostDataToSFDC(data,TaskAction.Create,"","");
                }
                else if(action=="Approved")
                {
                    List<SearchParams> searchParameters = new List<SearchParams>();
                    searchParameters.Add(new SearchParams
                    {
                        PropertyName = "IcertisCLM__TaskSysId__c",
                        Value = associationId,
                        Operator = CustomizationConstant.EqualOperator
                    });
                    searchParameters.Add(new SearchParams
                    {
                        PropertyName = "Status",
                        Value = TaskStatus.Completed.ToString(),
                        Operator = CustomizationConstant.NotEqualOperator
                    });
                    JArray arrayTask = GetTaskListByQuery(searchParameters);
                    foreach (var item in arrayTask.Children<JObject>())
                    {
                        string updateJsonPayload = "{'"+ CustomizationConstant .StatusField+ "':'"+TaskStatus.Completed.ToString()+"'}";
                        PostDataToSFDC(updateJsonPayload, TaskAction.Update, "", Convert.ToString(item.GetValue("Id")));
                        
                    }

                }
            }
            else if(typeOfContract == "Agreement")
            {
                if (action == "Publish")
                {
                    subject = $"Created agreement for {agreementCode}";
                    string data = GetPayLoadForAgreement(activityDate, assignedTo, subject, agreementId);
                    PostDataToSFDC(data, TaskAction.Create, "", "");
                }
                if (action == "Start Colloboration" || action == "Approved")
                {
                    List<SearchParams> searchParameters = new List<SearchParams>();
                    searchParameters.Add(new SearchParams
                    {
                        PropertyName = "IcertisCLM__Instance_Id__c",
                        Value = agreementId,
                        Operator = CustomizationConstant.EqualOperator
                    });
                    searchParameters.Add(new SearchParams
                    {
                        PropertyName = "Status",
                        Value = TaskStatus.Completed.ToString(),
                        Operator = CustomizationConstant.NotEqualOperator
                    });

                    JArray arrayTask = GetTaskListByQuery(searchParameters);
                    foreach (var item in arrayTask.Children<JObject>())
                    {
                        string updateJsonPayload = "{'" + CustomizationConstant.StatusField + "':'" + TaskStatus.Completed.ToString() + "'}";
                        PostDataToSFDC(updateJsonPayload, TaskAction.Update, "", Convert.ToString(item.GetValue("Id")));

                    }
                    if (action == "Start Colloboration")
                    {
                        subject = $"Colloboration topic for {agreementCode}";
                        string data = GetPayLoadForAgreement(activityDate, assignedTo, subject, agreementId);
                        PostDataToSFDC(data, TaskAction.Create, "", "");
                    }
                }

            }

            
        }
    }
}
