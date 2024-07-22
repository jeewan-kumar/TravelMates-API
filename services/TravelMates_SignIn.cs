
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;

namespace TravelMate_Api.services
{
    public class TravelMates_SignIn
    {
        dbServices ds = new dbServices(); // Assuming this is your database service

        public async Task<responseData> UserSignIn_UsingPassword(requestData rData)
        {
            responseData resData = new responseData();
            try
            {
                bool isUsingEmail = rData.addInfo.ContainsKey("email") && rData.addInfo.ContainsKey("password");
                bool isUsingPhone = rData.addInfo.ContainsKey("phone_number") && rData.addInfo.ContainsKey("password");

                if (!isUsingEmail && !isUsingPhone)
                {
                    resData.rData["rMessage"] = "Invalid request. Please provide either email/password or phone_number/password.";
                    resData.rData["rCode"] = 1;
                    return resData;
                }

                // Determine query field and value based on input type
                string queryField = isUsingEmail ? "email" : "phone_number";
                string fieldValue = isUsingEmail ? rData.addInfo["email"].ToString() : rData.addInfo["phone_number"].ToString();

                // Fetch user details from database
                MySqlParameter[] myParam = new MySqlParameter[]
                {
                    new MySqlParameter($"@{queryField}", fieldValue)
                };

                var query = $@"SELECT user_id, password, email_verified, phone_verified FROM pc_student.TravelMates_Users WHERE {queryField} = @{queryField}";
                List<List<object[]>> dbDataList = ds.executeSQL(query, myParam);

                if (dbDataList != null && dbDataList.Count > 0)
                {
                    List<object[]> dbData = dbDataList[0];

                    if (dbData.Count > 0)
                    {
                        int userId = Convert.ToInt32(dbData[0][0]);
                        string storedPassword = dbData[0][1].ToString();
                        bool emailVerified = Convert.ToBoolean(dbData[0][2]);
                        bool phoneVerified = Convert.ToBoolean(dbData[0][3]);

                        // Check if email is verified
                        if (isUsingEmail && !emailVerified)
                        {
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = "Email not verified. Please verify your email.";
                            return resData;
                        }

                        // Check if phone number is verified
                        if (isUsingPhone && !phoneVerified)
                        {
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = "Phone number not verified. Please verify your phone number.";
                            return resData;
                        }

                        // Check if the password is correct
                        if (storedPassword == rData.addInfo["password"].ToString())
                        {
                            // Generate JWT token
                            var token = GenerateJwtToken(userId, fieldValue);

                            // Store the token in the database
                            StoreToken(userId, token);

                            resData.rData["rCode"] = 0;
                            resData.rData["rMessage"] = "Login successful.";
                            resData.rData["user_id"] = userId;
                            resData.rData["token"] = token;
                        }
                        else
                        {
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = "Incorrect password.";
                        }
                    }
                    else
                    {
                        resData.rData["rCode"] = 1;
                        resData.rData["rMessage"] = "User not found. Please sign up.";
                    }
                }
                else
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "User not found. Please sign up.";
                }
            }
            catch (Exception ex)
            {
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
                // Log the exception
            }
            return resData;
        }

        private string GenerateJwtToken(int userId, string identifier)
        {
            byte[] keyBytes = new byte[32]; // 256 bits = 32 bytes
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(keyBytes);
            }
            var base64Key = Convert.ToBase64String(keyBytes);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(base64Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, identifier),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", userId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "yourIssuer",
                audience: "yourAudience",
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void StoreToken(int userId, string token)
        {
            DateTime issuedAt = DateTime.Now;
            DateTime expiresAt = issuedAt.AddHours(2);

            MySqlParameter[] insertParams = new MySqlParameter[]
            {
                new MySqlParameter("@user_id", userId),
                new MySqlParameter("@token", token),
                new MySqlParameter("@issued_at", issuedAt),
                new MySqlParameter("@expires_at", expiresAt),
                new MySqlParameter("@revoked", false)
            };

            var query = @"INSERT INTO pc_student.TravelMates_TokenStore (user_id, token, issued_at, expires_at, revoked) 
                          VALUES (@user_id, @token, @issued_at, @expires_at, @revoked)";

            ds.ExecuteSQLName(query, insertParams);
        }


        public async Task<responseData> GenerateOtpForLogin(requestData rData)
        {
            responseData resData = new responseData();
            try
            {
                bool isUsingEmail = rData.addInfo.ContainsKey("email") && !string.IsNullOrWhiteSpace(rData.addInfo["email"].ToString());
                bool isUsingPhone = rData.addInfo.ContainsKey("phone_number") && !string.IsNullOrWhiteSpace(rData.addInfo["phone_number"].ToString());

                if (!isUsingEmail && !isUsingPhone)
                {
                    resData.rData["rMessage"] = "Invalid request. Please provide either email or phone_number.";
                    resData.rData["rCode"] = 1;
                    return resData;
                }

                string queryField = isUsingEmail ? "email" : "phone_number";
                string fieldValue = isUsingEmail ? rData.addInfo["email"].ToString() : rData.addInfo["phone_number"].ToString();

                // Fetch user details from database
                MySqlParameter[] myParam = new MySqlParameter[]
                {
            new MySqlParameter($"@{queryField}", fieldValue)
                };

                var query = $@"SELECT user_id, email_verified, phone_verified FROM pc_student.TravelMates_Users WHERE {queryField} = @{queryField}";
                List<List<object[]>> dbDataList = ds.executeSQL(query, myParam);

                if (dbDataList != null && dbDataList.Count > 0)
                {
                    List<object[]> dbData = dbDataList[0];

                    if (dbData.Count > 0)
                    {
                        int userId = Convert.ToInt32(dbData[0][0]);
                        bool emailVerified = Convert.ToBoolean(dbData[0][1]);
                        bool phoneVerified = Convert.ToBoolean(dbData[0][2]);

                        // Check if email or phone is verified
                        if (isUsingEmail && !emailVerified)
                        {
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = "Email not verified. Please verify your email.";
                            return resData;
                        }

                        if (isUsingPhone && !phoneVerified)
                        {
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = "Phone number not verified. Please verify your phone number.";
                            return resData;
                        }

                        // Generate OTP
                        string otp = GenerateOtp();

                        // Store OTP in database and get the inserted ID
                        int otpId = StoreOtp(userId, otp, isUsingEmail);

                        // Log the inserted OTP ID
                        string idType = isUsingEmail ? "email_id" : "phone_id";
                        Console.WriteLine($"{idType} for OTP: {otpId}");

                        // Send OTP to user's email or phone number
                        bool isSent = isUsingEmail ? await SendOtpToEmail(fieldValue, otp) : await SendOtpToPhone(fieldValue, otp);

                        if (isSent)
                        {
                            resData.rData["rCode"] = 0;
                            resData.rData["rMessage"] = "OTP sent successfully.";
                        }
                        else
                        {
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = "Failed to send OTP. Please try again.";
                        }
                    }
                    else
                    {
                        resData.rData["rCode"] = 1;
                        resData.rData["rMessage"] = "User not found. Please sign up.";
                    }
                }
                else
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "User not found. Please sign up.";
                }
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
                // Log the exception
                Console.WriteLine(ex.ToString());
            }
            return resData;
        }

        private string GenerateOtp()
        {
            Random rand = new Random();
            return rand.Next(100000, 999999).ToString(); // Generates a 4-digit OTP
        }

        private int StoreOtp(int userId, string otp, bool isUsingEmail)
        {
            string tableName = isUsingEmail ? "TravelMates_EmailOTP" : "TravelMates_PhoneOTP";
            DateTime expiresAt = DateTime.Now.AddMinutes(5); // Example expiration time of 5 minutes
            MySqlParameter[] insertParams = new MySqlParameter[]
            {
                new MySqlParameter("@user_id", userId),
                new MySqlParameter("@otp", otp),
                new MySqlParameter("@created_at", DateTime.Now),
                new MySqlParameter("@expires_at", expiresAt)
            };

            var query = $@"INSERT INTO pc_student.{tableName} (user_id, otp, created_at, expires_at) 
                  VALUES (@user_id, @otp, @created_at, @expires_at)
                  ON DUPLICATE KEY UPDATE otp = @otp, created_at = @created_at, expires_at = @expires_at";

            ds.executeSQL(query, insertParams);

            // Retrieve the last inserted ID (assuming the table has an AUTO_INCREMENT primary key)
            var idQuery = $@"SELECT LAST_INSERT_ID()";
            List<List<object[]>> idResult = ds.executeSQL(idQuery, null);

            if (idResult != null && idResult.Count > 0 && idResult[0].Count > 0)
            {
                return Convert.ToInt32(idResult[0][0][0]);
            }

            return 0; // Return 0 if unable to fetch the inserted ID
        }

        private async Task<bool> SendOtpToEmail(string email, string otp)
        {
            try
            {
                // Implement email sending logic here
                // Example: Use an email service API like SendGrid
                Console.WriteLine($"OTP for {email}: {otp}"); // For testing purposes
                return true; // Assuming OTP is sent successfully
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send OTP to {email}: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendOtpToPhone(string phoneNumber, string otp)
        {
            try
            {
                // Implement SMS sending logic here
                // Example: Use Twilio or any SMS service API
                Console.WriteLine($"OTP for {phoneNumber}: {otp}"); // For testing purposes
                return true; // Assuming OTP is sent successfully
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send OTP to {phoneNumber}: {ex.Message}");
                return false;
            }
        }
        public async Task<responseData> VerifyOtpForLogin(requestData rData)
        {
            responseData resData = new responseData();
            try
            {
                bool isUsingEmail = rData.addInfo.ContainsKey("email") && !string.IsNullOrWhiteSpace(rData.addInfo["email"].ToString());
                bool isUsingPhone = rData.addInfo.ContainsKey("phone_number") && !string.IsNullOrWhiteSpace(rData.addInfo["phone_number"].ToString());

                if (!isUsingEmail && !isUsingPhone)
                {
                    resData.rData["rMessage"] = "Invalid request. Please provide either email or phone_number.";
                    resData.rData["rCode"] = 1;
                    return resData;
                }

                string queryField = isUsingEmail ? "email" : "phone_number";
                string fieldValue = isUsingEmail ? rData.addInfo["email"].ToString() : rData.addInfo["phone_number"].ToString();
                string otp = rData.addInfo["otp"].ToString();

                // Fetch user and OTP details from database
                MySqlParameter[] myParam = new MySqlParameter[]
                {
            new MySqlParameter($"@{queryField}", fieldValue)
                };

                var userQuery = $@"SELECT user_id FROM pc_student.TravelMates_Users WHERE {queryField} = @{queryField}";
                List<List<object[]>> dbDataList = ds.executeSQL(userQuery, myParam);

                if (dbDataList != null && dbDataList.Count > 0)
                {
                    List<object[]> dbData = dbDataList[0];

                    if (dbData.Count > 0)
                    {
                        int userId = Convert.ToInt32(dbData[0][0]);

                        string tableName = isUsingEmail ? "TravelMates_EmailOTP" : "TravelMates_PhoneOTP";
                        MySqlParameter[] otpParam = new MySqlParameter[]
                        {
                            new MySqlParameter("@user_id", userId),
                            new MySqlParameter("@otp", otp)
                        };

                        var otpQuery = $@"SELECT expires_at, used FROM pc_student.{tableName} WHERE user_id = @user_id AND otp = @otp";
                        List<List<object[]>> otpDataList = ds.executeSQL(otpQuery, otpParam);

                        if (otpDataList != null && otpDataList.Count > 0)
                        {
                            List<object[]> otpData = otpDataList[0];

                            if (otpData.Count > 0)
                            {
                                DateTime expiresAt = Convert.ToDateTime(otpData[0][0]);
                                bool used = Convert.ToBoolean(otpData[0][1]);

                                if (used)
                                {
                                    resData.rData["rCode"] = 1;
                                    resData.rData["rMessage"] = "OTP has already been used.";
                                    return resData;
                                }

                                if (DateTime.Now > expiresAt)
                                {
                                    resData.rData["rCode"] = 1;
                                    resData.rData["rMessage"] = "OTP has expired.";
                                    return resData;
                                }
                                string token = GenerateJwtToken(userId, fieldValue);

                                StoreToken(userId, token);
                                // Mark OTP as used
                                MarkOtpAsUsed(userId, otp, isUsingEmail);

                                resData.rData["rCode"] = 0;
                                resData.rData["rMessage"] = "Sign IN successfully.";
                                resData.rData["user_id"] = userId;
                                resData.rData["token"] = token;
                            }
                            else
                            {
                                resData.rData["rCode"] = 1;
                                resData.rData["rMessage"] = "Invalid OTP.";
                            }
                        }
                        else
                        {
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = "Invalid OTP.";
                        }
                    }
                    else
                    {
                        resData.rData["rCode"] = 1;
                        resData.rData["rMessage"] = "User not found.";
                    }
                }
                else
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "User not found.";
                }
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
                // Log the exception
                Console.WriteLine(ex.ToString());
            }
            return resData;
        }


        private void MarkOtpAsUsed(int userId, string otp, bool isUsingEmail)
        {
            string tableName = isUsingEmail ? "TravelMates_EmailOTP" : "TravelMates_PhoneOTP";
            MySqlParameter[] updateParams = new MySqlParameter[]
            {
                new MySqlParameter("@user_id", userId),
                new MySqlParameter("@otp", otp)
            };

            var query = $@"UPDATE pc_student.{tableName} SET used = TRUE WHERE user_id = @user_id AND otp = @otp";
            ds.executeSQL(query, updateParams);
        }

        public async Task<responseData> ForgotPassword(requestData rData)
        {
            responseData resData = new responseData();
            try
            {
                bool isUsingEmail = rData.addInfo.ContainsKey("email") && !string.IsNullOrWhiteSpace(rData.addInfo["email"].ToString());
                bool isUsingPhone = rData.addInfo.ContainsKey("phone_number") && !string.IsNullOrWhiteSpace(rData.addInfo["phone_number"].ToString());

                if (!isUsingEmail && !isUsingPhone)
                {
                    resData.rData["rMessage"] = "Invalid request. Please provide either email or phone_number.";
                    resData.rData["rCode"] = 1;
                    return resData;
                }

                string queryField = isUsingEmail ? "email" : "phone_number";
                string fieldValue = isUsingEmail ? rData.addInfo["email"].ToString() : rData.addInfo["phone_number"].ToString();

                // Fetch user details from database
                MySqlParameter[] myParam = new MySqlParameter[]
                {
                    new MySqlParameter($"@{queryField}", fieldValue)
                };

                var query = $@"SELECT user_id, email_verified, phone_verified FROM pc_student.TravelMates_Users WHERE {queryField} = @{queryField}";
                List<List<object[]>> dbDataList = ds.executeSQL(query, myParam);

                if (dbDataList != null && dbDataList.Count > 0)
                {
                    List<object[]> dbData = dbDataList[0];

                    if (dbData.Count > 0)
                    {
                        int userId = Convert.ToInt32(dbData[0][0]);
                        bool emailVerified = Convert.ToBoolean(dbData[0][1]);
                        bool phoneVerified = Convert.ToBoolean(dbData[0][2]);

                        // Check if email or phone is verified
                        if (isUsingEmail && !emailVerified)
                        {
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = "Email not verified. Please verify your email.";
                            
                            return resData;
                        }

                        if (isUsingPhone && !phoneVerified)
                        {
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = "Phone number not verified. Please verify your phone number.";
                           
                            return resData;
                        }

                        // Generate OTP
                        string otp = GenerateOtp();

                        // Store OTP in database and get the inserted ID
                        int otpId = StoreOtp(userId, otp, isUsingEmail);

                        // Log the inserted OTP ID
                        string idType = isUsingEmail ? "email_id" : "phone_id";
                        Console.WriteLine($"{idType} for OTP: {otpId}");

                        // Send OTP to user's email or phone number
                        bool isSent = isUsingEmail ? await SendOtpToEmail(fieldValue, otp) : await SendOtpToPhone(fieldValue, otp);

                        if (isSent)
                        {
                            resData.rData["rCode"] = 0;
                            resData.rData["rMessage"] = "OTP sent successfully.";
                            resData.rData["user_Id"] = userId;
                        }
                        else
                        {
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = "Failed to send OTP. Please try again.";
                        }
                    }
                    else
                    {
                        resData.rData["rCode"] = 1;
                        resData.rData["rMessage"] = "User not found. Please sign up.";
                    }
                }
                else
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "User not found. Please sign up.";
                }
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
                // Log the exception
                Console.WriteLine(ex.ToString());
            }
            return resData;
        }


        public async Task<responseData> VerifyOtpForForgotPassword(requestData rData)
        {
            responseData resData = new responseData();
            try
            {
                bool isUsingEmail = rData.addInfo.ContainsKey("email") && !string.IsNullOrWhiteSpace(rData.addInfo["email"].ToString());
                bool isUsingPhone = rData.addInfo.ContainsKey("phone_number") && !string.IsNullOrWhiteSpace(rData.addInfo["phone_number"].ToString());

                if (!isUsingEmail && !isUsingPhone)
                {
                    resData.rData["rMessage"] = "Invalid request. Please provide either email or phone_number.";
                    resData.rData["rCode"] = 1;
                    return resData;
                }

                string queryField = isUsingEmail ? "email" : "phone_number";
                string fieldValue = isUsingEmail ? rData.addInfo["email"].ToString() : rData.addInfo["phone_number"].ToString();
                string otp = rData.addInfo["otp"].ToString();

                // Fetch user and OTP details from database
                MySqlParameter[] myParam = new MySqlParameter[]
                {
                    new MySqlParameter($"@{queryField}", fieldValue)
                };

                var userQuery = $@"SELECT user_id FROM pc_student.TravelMates_Users WHERE {queryField} = @{queryField}";
                List<List<object[]>> dbDataList = ds.executeSQL(userQuery, myParam);

                if (dbDataList != null && dbDataList.Count > 0)
                {
                    List<object[]> dbData = dbDataList[0];

                    if (dbData.Count > 0)
                    {
                        int userId = Convert.ToInt32(dbData[0][0]);

                        string tableName = isUsingEmail ? "TravelMates_EmailOTP" : "TravelMates_PhoneOTP";
                        MySqlParameter[] otpParam = new MySqlParameter[]
                        {
                            new MySqlParameter("@user_id", userId),
                            new MySqlParameter("@otp", otp)
                        };

                        var otpQuery = $@"SELECT expires_at, used FROM pc_student.{tableName} WHERE user_id = @user_id AND otp = @otp";
                        List<List<object[]>> otpDataList = ds.executeSQL(otpQuery, otpParam);

                        if (otpDataList != null && otpDataList.Count > 0)
                        {
                            List<object[]> otpData = otpDataList[0];

                            if (otpData.Count > 0)
                            {
                                DateTime expiresAt = Convert.ToDateTime(otpData[0][0]);
                                bool used = Convert.ToBoolean(otpData[0][1]);

                                if (used)
                                {
                                    resData.rData["rCode"] = 1;
                                    resData.rData["rMessage"] = "OTP has already been used.";
                                    return resData;
                                }

                                if (DateTime.Now > expiresAt)
                                {
                                    resData.rData["rCode"] = 1;
                                    resData.rData["rMessage"] = "OTP has expired.";
                                    return resData;
                                }



                                // Mark OTP as used
                                MarkOtpAsUsed(userId, otp, isUsingEmail);

                                resData.rData["rCode"] = 0;
                                resData.rData["rMessage"] = "Otp verified successfully.";
                                resData.rData["user_id"] = userId;

                            }
                            else
                            {
                                resData.rData["rCode"] = 1;
                                resData.rData["rMessage"] = "Invalid OTP.";
                            }
                        }
                        else
                        {
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = "Invalid OTP.";
                        }
                    }
                    else
                    {
                        resData.rData["rCode"] = 1;
                        resData.rData["rMessage"] = "User not found.";
                    }
                }
                else
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "User not found.";
                }
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
                // Log the exception
                Console.WriteLine(ex.ToString());
            }
            return resData;
        }
        public async Task<responseData> ResetPassword(requestData rData)
        {
            responseData resData = new responseData();
            try
            {
                // Check for email or phone number
                bool isUsingEmail = rData.addInfo.ContainsKey("email") && !string.IsNullOrWhiteSpace(rData.addInfo["email"].ToString());
                bool isUsingPhone = rData.addInfo.ContainsKey("phone_number") && !string.IsNullOrWhiteSpace(rData.addInfo["phone_number"].ToString());
                bool hasUserId = rData.addInfo.ContainsKey("user_id") && !string.IsNullOrWhiteSpace(rData.addInfo["user_id"].ToString());

                if (!hasUserId || (!isUsingEmail && !isUsingPhone))
                {
                    resData.rData["rMessage"] = "Invalid request. Please provide user_id and either email or phone number.";
                    resData.rData["rCode"] = 1;
                    return resData;
                }

                int userId = Convert.ToInt32(rData.addInfo["user_id"].ToString());
                string newPassword = rData.addInfo["new_password"].ToString();

                string queryField = isUsingEmail ? "email" : "phone_number";
                string fieldValue = isUsingEmail ? rData.addInfo["email"].ToString() : rData.addInfo["phone_number"].ToString();

                // Fetch user details from database
                MySqlParameter[] myParam = new MySqlParameter[]
                {
            new MySqlParameter("@user_id", userId),
            new MySqlParameter($"@{queryField}", fieldValue)
                };

                var query = $@"SELECT user_id FROM pc_student.TravelMates_Users WHERE user_id = @user_id AND {queryField} = @{queryField}";
                List<List<object[]>> dbDataList = ds.executeSQL(query, myParam);

                if (dbDataList != null && dbDataList.Count > 0)
                {
                    List<object[]> dbData = dbDataList[0];

                    if (dbData.Count > 0)
                    {
                        // Update password
                        MySqlParameter[] updateParam = new MySqlParameter[]
                        {
                    new MySqlParameter("@user_id", userId),
                    new MySqlParameter("@new_password", newPassword) // Ensure to hash the password before storing it
                        };

                        var updateQuery = "UPDATE pc_student.TravelMates_Users SET password = @new_password WHERE user_id = @user_id";
                        int rowsAffected = ds.executeSQL(updateQuery, updateParam).Count; // Fix: Ensure executeSQL returns rows affected count

                        if (rowsAffected > 0)
                        {
                            resData.rData["rCode"] = 0;
                            resData.rData["rMessage"] = "Password reset successfully.";
                             resData.rData["user_Id"] = userId;
                        }
                        else
                        {
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = "Failed to reset password. Please try again.";
                        }
                    }
                    else
                    {
                        resData.rData["rCode"] = 1;
                        resData.rData["rMessage"] = "User ID does not match the provided email or phone number.";
                    }
                }
                else
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "User not found.";
                }
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
                // Log the exception
                Console.WriteLine(ex.ToString());
            }
            return resData;
        }


    }

}

