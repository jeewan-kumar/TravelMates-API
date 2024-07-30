using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace COMMON_PROJECT_STRUCTURE_API.services
{
    public class TravelMates_UserProfiles
    {
        dbServices ds = new dbServices();
        public async Task<responseData> GetRandomUserProfiles(requestData req)
        {
            var resData = new responseData();
            try
            {
                // List of SQL parameters for filtering
                var filterParams = new List<MySqlParameter>();
                var filters = new List<string>();

                // Add filters based on the provided request data
                if (req.addInfo.TryGetValue("location", out var location) && !string.IsNullOrEmpty(location.ToString()))
                {
                    filters.Add("location = @location");
                    filterParams.Add(new MySqlParameter("@location", location.ToString()));
                }
                if (req.addInfo.TryGetValue("gender", out var gender) && !string.IsNullOrEmpty(gender.ToString()))
                {
                    filters.Add("gender = @gender");
                    filterParams.Add(new MySqlParameter("@gender", gender.ToString()));
                }
                if (req.addInfo.TryGetValue("travel_preferences", out var travelPreferences) && !string.IsNullOrEmpty(travelPreferences.ToString()))
                {
                    filters.Add("travel_preferences = @travel_preferences");
                    filterParams.Add(new MySqlParameter("@travel_preferences", travelPreferences.ToString()));
                }
                if (req.addInfo.TryGetValue("travel_types", out var travelTypes) && !string.IsNullOrEmpty(travelTypes.ToString()))
                {
                    filters.Add("travel_types = @travel_types");
                    filterParams.Add(new MySqlParameter("@travel_types", travelTypes.ToString()));
                }
                if (req.addInfo.TryGetValue("traveling_intentions", out var travelingIntentions) && !string.IsNullOrEmpty(travelingIntentions.ToString()))
                {
                    filters.Add("traveling_intentions = @traveling_intentions");
                    filterParams.Add(new MySqlParameter("@traveling_intentions", travelingIntentions.ToString()));
                }
                if (req.addInfo.TryGetValue("job_title", out var jobTitle) && !string.IsNullOrEmpty(jobTitle.ToString()))
                {
                    filters.Add("job_title = @job_title");
                    filterParams.Add(new MySqlParameter("@job_title", jobTitle.ToString()));
                }
                if (req.addInfo.TryGetValue("workplace", out var workplace) && !string.IsNullOrEmpty(workplace.ToString()))
                {
                    filters.Add("workplace = @workplace");
                    filterParams.Add(new MySqlParameter("@workplace", workplace.ToString()));
                }
                if (req.addInfo.TryGetValue("education", out var education) && !string.IsNullOrEmpty(education.ToString()))
                {
                    filters.Add("education = @education");
                    filterParams.Add(new MySqlParameter("@education", education.ToString()));
                }

                // Add age filter if specified
                if (req.addInfo.TryGetValue("min_age", out var minAge) && !string.IsNullOrEmpty(minAge.ToString()) &&
                    req.addInfo.TryGetValue("max_age", out var maxAge) && !string.IsNullOrEmpty(maxAge.ToString()))
                {
                    filters.Add("TIMESTAMPDIFF(YEAR, date_of_birth, CURDATE()) BETWEEN @min_age AND @max_age");
                    filterParams.Add(new MySqlParameter("@min_age", Convert.ToInt32(minAge)));
                    filterParams.Add(new MySqlParameter("@max_age", Convert.ToInt32(maxAge)));
                }

                // Construct the SQL query with the filters
                var filterQuery = filters.Count > 0 ? "WHERE " + string.Join(" AND ", filters) : "";
                var selectQuery = $@"
                    SELECT full_name, profile_picture, 
                        TIMESTAMPDIFF(YEAR, date_of_birth, CURDATE()) AS age, 
                        job_title
                    FROM pc_student.TravelMates_Users 
                    {filterQuery}
                    ORDER BY RAND()";

                // Execute the query and retrieve results
                var selectResult = ds.executeSQL(selectQuery, filterParams.ToArray());

                if (selectResult == null || selectResult.Count == 0)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "No users found";
                }
                else
                {
                    resData.rData["rCode"] = 0;
                    resData.rData["rMessage"] = "Users retrieved successfully";
                    resData.rData["users"] = selectResult;
                }
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
            }

            return resData;
        }
        


        public async Task<responseData> ReadProfile(requestData req)
        {
            responseData resData = new responseData();
            try
            {
                MySqlParameter[] Params = new MySqlParameter[]
              {
                        new MySqlParameter("@user_id", req.addInfo["user_id"]),

              };
                var selectQuery = @"SELECT * FROM pc_student.TravelMates_Users where user_id=@user_id";

                var selectResult = ds.executeSQL(selectQuery, Params);
                if (selectResult[0].Count() == 0)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "No UserProfile found";
                }
                else
                {
                    resData.rData["rCode"] = 0;
                    resData.rData["rMessage"] = "Userprofile retrieved Successfully";
                    resData.rData["lessons"] = selectResult;
                }
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
            }
            return resData;
        }
        public async Task<responseData> UpdateProfile(requestData req)
        {
            responseData resData = new responseData();
            try
            {
                // Check if addInfo and necessary keys are present
                if (req.addInfo == null || !req.addInfo.ContainsKey("user_id"))
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Invalid request data";
                    return resData;
                }

                // Prepare parameters for the SQL query
                List<MySqlParameter> updateParams = new List<MySqlParameter>
        {
            new MySqlParameter("@user_id", req.addInfo["user_id"].ToString()),
            new MySqlParameter("@profile_picture", req.addInfo["profile_picture"]?.ToString() ?? string.Empty),
            new MySqlParameter("@full_name", req.addInfo["full_name"]?.ToString() ?? string.Empty),
            new MySqlParameter("@date_of_birth", req.addInfo["date_of_birth"]?.ToString() ?? string.Empty),
            new MySqlParameter("@location", req.addInfo["location"]?.ToString() ?? string.Empty),
            new MySqlParameter("@gender", req.addInfo["gender"]?.ToString() ?? string.Empty),
            new MySqlParameter("@travel_preferences", req.addInfo["travel_preferences"]?.ToString() ?? string.Empty),
            new MySqlParameter("@travel_types", req.addInfo["travel_types"]?.ToString() ?? string.Empty),
            new MySqlParameter("@traveling_intentions", req.addInfo["traveling_intentions"]?.ToString() ?? string.Empty),
            new MySqlParameter("@job_title", req.addInfo["job_title"]?.ToString() ?? string.Empty),
            new MySqlParameter("@workplace", req.addInfo["workplace"]?.ToString() ?? string.Empty),
            new MySqlParameter("@education", req.addInfo["education"]?.ToString() ?? string.Empty),
            new MySqlParameter("@religious_beliefs", req.addInfo["religious_beliefs"]?.ToString() ?? string.Empty),
            new MySqlParameter("@interests", req.addInfo["interests"]?.ToString() ?? string.Empty),
            new MySqlParameter("@bio", req.addInfo["bio"]?.ToString() ?? string.Empty),
        };

                // SQL query to update the record
                var updateQuery = @"
            UPDATE pc_student.TravelMates_Users 
            SET profile_picture = @profile_picture,
                full_name = @full_name, 
                date_of_birth = @date_of_birth, 
                location = @location,
                gender = @gender, 
                travel_preferences = @travel_preferences,
                travel_types = @travel_types,
                traveling_intentions = @traveling_intentions,
                job_title = @job_title,
                workplace = @workplace,
                education = @education,
                religious_beliefs = @religious_beliefs,
                interests = @interests,
                bio = @bio
            WHERE user_id = @user_id";

                // Execute SQL update query
                var updateResult = ds.executeSQL(updateQuery, updateParams.ToArray());

                // Check if the update was successful
                if (updateResult == null || !updateResult.Any())
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Unsuccessful profile update";
                }
                else
                {
                    resData.rData["rCode"] = 0;
                    resData.rData["rMessage"] = "Profile updated successfully";
                }
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
                // Log exception details here for debugging
            }
            return resData;
        }


        public async Task<responseData> DeleteProfile(requestData req)
        {
            responseData resData = new responseData();
            try
            {
                // Create MySQL parameters for the delete query
                MySqlParameter[] deleteParams = new MySqlParameter[]
                {
            new MySqlParameter("@id", req.addInfo["id"].ToString()),
              new MySqlParameter("@status",0)
                };

                // Define the delete query
                var query = @"DELETE FROM pc_student.TravelMates_Users WHERE user_id = @user_id";
                //var query = @"UPDATE pc_student.Skillup_UserProfile SET status = @status WHERE id = @id";

                // Execute the delete query
                var deleteResult = ds.executeSQL(query, deleteParams);

                // Check the result of the delete operation
                if (deleteResult[0].Count() == 0 && deleteResult == null)
                {
                    resData.rData["rCode"] = 1; // Unsuccessful
                    resData.rData["rMessage"] = "Profile Unsuccessful delete";
                }
                else
                {
                    resData.rData["rCode"] = 0; // Successful
                    resData.rData["rMessage"] = "profile delete Successful";
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the operation
                resData.rData["rCode"] = 1; // Indicate an error
                resData.rData["rMessage"] = "Error: " + ex.Message;
            }

            // Return the response data
            return resData;
        }

        public async Task<responseData> UpdateUserProfileImage(requestData req)
        {
            responseData resData = new responseData();
            try
            {
                byte[] imageData = null;

                // Check if the request contains a new image file to update
                if (req.addInfo.ContainsKey("profile_picture") && !string.IsNullOrEmpty(req.addInfo["profile_picture"].ToString()))
                {
                    var filePath = req.addInfo["profile_picture"].ToString();
                    if (File.Exists(filePath))
                    {
                        imageData = File.ReadAllBytes(filePath);
                    }
                    else
                    {
                        resData.rData["rCode"] = 1;
                        resData.rData["rMessage"] = "File not found: " + filePath;
                        return resData;
                    }
                }
                else
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "No profile picture provided";
                    return resData;
                }

                // Parameters for SQL query
                MySqlParameter[] updateParams = new MySqlParameter[]
                {
            new MySqlParameter("@user_id", req.addInfo["user_id"].ToString()),
            new MySqlParameter("@profile_picture", MySqlDbType.Blob) { Value = imageData }
                };

                // SQL query to update record
                var updateQuery = @"UPDATE pc_student.TravelMates_Users SET profile_picture = @profile_picture WHERE user_id = @user_id";

                // Execute SQL update query
                var updateResult = ds.executeSQL(updateQuery, updateParams);

                // Check if update was successful
                if (updateResult == null || !updateResult.Any())
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Unsuccessful profile picture update";
                }
                else
                {
                    resData.rData["rCode"] = 0;
                    resData.rData["rMessage"] = "Profile picture updated successfully";
                }
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
            }
            return resData;
        }
        public async Task<responseData> GetUserProfile(requestData req)
        {
            responseData resData = new responseData();
            try
            {
                MySqlParameter[] Params = new MySqlParameter[]
                {
                    new MySqlParameter("@skillup_id", req.addInfo["skillup_id"]),
                };

                var selectQuery = @"
                    SELECT up.profile_picture, up.first_name, up.last_name, up.date_of_birth, up.bio, us.email, us.phone_number,
                           CONCAT(up.first_name, ' ', up.last_name) AS name, up.gender
                    FROM pc_student.Skillup_UserProfile up
                    JOIN pc_student.Skillup_UserSignUp us ON up.skillup_id = us.skillup_id
                    WHERE up.skillup_id = @skillup_id";

                var selectResult = ds.executeSQL(selectQuery, Params);
                if (selectResult == null || selectResult.Count == 0)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "No UserProfile found";
                }
                else
                {
                    resData.rData["rCode"] = 0;
                    resData.rData["rMessage"] = "User profile retrieved successfully";
                    resData.rData["profile"] = selectResult[0];
                }
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
            }
            return resData;
        }
        public async Task<responseData> UpdateUserProfile(requestData req)
        {
            responseData resData = new responseData();

            try
            {
                // Validate input parameters
                if (!req.addInfo.ContainsKey("user_id"))
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "User ID is required";
                    return resData;
                }


                List<MySqlParameter> updateParams = new List<MySqlParameter>
        {
            new MySqlParameter("@user_id", req.addInfo["user_id"].ToString()),
            new MySqlParameter("@profile_picture", req.addInfo["profile_picture"].ToString()),
            new MySqlParameter("@first_name", req.addInfo["first_name"].ToString()),
            new MySqlParameter("@last_name", req.addInfo["last_name"].ToString()),
            new MySqlParameter("@date_of_birth", req.addInfo["date_of_birth"].ToString()),
            new MySqlParameter("@bio", req.addInfo["bio"].ToString()),
            new MySqlParameter("@email", req.addInfo["email"].ToString()),
            new MySqlParameter("@phone_number", req.addInfo["phone_number"].ToString()),
            new MySqlParameter("@gender", req.addInfo["gender"].ToString())
        };


                string updateQuery = @"
            UPDATE pc_student.TravelMates_Users 
            SET
                up.profile_picture = @profile_picture,
                up.first_name = @first_name,
                up.last_name = @last_name,
                up.date_of_birth = @date_of_birth,
                up.bio = @bio,
                us.email = @email,
                us.phone_number = @phone_number,
                up.gender = @gender
            WHERE
                up.user_id = @user_id;
        ";

                // Execute the update query
                var updateResult = ds.executeSQL(updateQuery, updateParams.ToArray());

                // Check if update was successful
                if (updateResult == null || updateResult.Count == 0)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Failed to update user profile";
                }
                else
                {
                    resData.rData["rCode"] = 0;
                    resData.rData["rMessage"] = "User profile updated successfully";
                }
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
            }

            return resData;
        }


    }
}

