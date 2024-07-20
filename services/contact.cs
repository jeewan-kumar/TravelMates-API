using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace COMMON_PROJECT_STRUCTURE_API.services
{
    public class contact
    {
        dbServices ds = new dbServices();
        public async Task<responseData> Contact(requestData rData)
        {
            responseData resData = new responseData();
            try
            {
                var query = @"select * from mydb.contact where EMAIL=@EMAIL";
                MySqlParameter[] myParam = new MySqlParameter[]
                {
                new MySqlParameter("@FIRST_NAME",rData.addInfo["FIRST_NAME"]),
                new MySqlParameter("@LAST_NAME",rData.addInfo["LAST_NAME"]),
                new MySqlParameter("@EMAIL", rData.addInfo["EMAIL"]) ,
                new MySqlParameter("@SUBJECT", rData.addInfo["SUBJECT"]) 
                };
                var dbData = ds.executeSQL(query, myParam);
                if (dbData[0].Count() > 0)
                {
                    resData.rData["rMessage"] = "Already Submitted";
                }
                else
                {
                    var sq=@"insert into mydb.contact(FIRST_NAME,LAST_NAME,EMAIL,SUBJECT ) values(@FIRST_NAME,@LAST_NAME,@EMAIL,@SUBJECT)";
                     MySqlParameter[] insertParams = new MySqlParameter[]
                    {
                         new MySqlParameter("@FIRST_NAME", rData.addInfo["FIRST_NAME"]),
                        new MySqlParameter("@LAST_NAME", rData.addInfo["LAST_NAME"]),
                        new MySqlParameter("@EMAIL", rData.addInfo["EMAIL"]) ,
                         new MySqlParameter("@SUBJECT", rData.addInfo["SUBJECT"]) 

                    };
                    var insertResult = ds.executeSQL(sq, insertParams);

                    resData.rData["rMessage"] = "Submitted";
                    
                }

            }
            catch (Exception ex)
            {

                resData.rData["rMessage"] = ex;
            }
            return resData;
        }

       
    }
}
