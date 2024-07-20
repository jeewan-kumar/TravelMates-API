using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace COMMON_PROJECT_STRUCTURE_API.services
{
    public class upload
    {
        dbServices ds = new dbServices();
        public async Task<responseData> Upload(requestData rData)
        {
            responseData resData = new responseData();
            try
            {
                var query = @"SELECT * FROM mydb.upload where INVENTOR=@INVENTOR";
                MySqlParameter[] myParam = new MySqlParameter[]
                {
                // new MySqlParameter("@INVENTION",rData.addInfo["INVENTION"]),
                new MySqlParameter("@INVENTOR",rData.addInfo["INVENTOR"]),
                // new MySqlParameter("@INVEN_DETAILS", rData.addInfo["INVEN_DETAILS"]) 
                };
                var dbData = ds.executeSQL(query, myParam);
                if (dbData[0].Count() > 0)
                {
                    resData.rData["rMessage"] = "Already Uploaded your Idea";
                }
                else
                {
                    var sq=@"insert into mydb.upload(INVENTION,INVENTOR,INVEN_DETAILS ) values(@INVENTION,@INVENTOR,@INVEN_DETAILS)";
                     MySqlParameter[] insertParams = new MySqlParameter[]
                    {
                         new MySqlParameter("@INVENTION", rData.addInfo["INVENTION"]),
                        new MySqlParameter("@INVENTOR", rData.addInfo["INVENTOR"]),
                        new MySqlParameter("@INVEN_DETAILS", rData.addInfo["INVEN_DETAILS"])  
                    };
                    var insertResult = ds.executeSQL(sq, insertParams);

                    resData.rData["rMessage"] = "Upload Successful";
                    
                }

            }
            catch (Exception ex)
            {

                throw;
            }
            return resData;
        }

       
    }
}
