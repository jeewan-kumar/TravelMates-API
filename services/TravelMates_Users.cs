using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Net;
using System.Net.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using System.Net.Sockets;
using RestSharp;
using System;
using System.Threading.Tasks;
using System.Text.Json;

namespace TravelMate_Api.services
{
    public class TravelMates_Users
    {
        dbServices ds = new dbServices();

        public async Task<responseData> TravelMates_UserSignUp(requestData req)
        {
            responseData resData = new responseData();
            try
            {
                string fullName = req.addInfo["full_name"].ToString();
                // Check if email or phone number already exists
                MySqlParameter[] myParam = new MySqlParameter[]
                {
                    new MySqlParameter("@phone_number", req.addInfo["phone_number"].ToString()),
                    new MySqlParameter("@email", req.addInfo["email"].ToString())
                };

                var query = @"SELECT * FROM pc_student.TravelMates_Users WHERE email=@email OR phone_number=@phone_number";
                List<List<object[]>> dbDataList = ds.executeSQL(query, myParam);

                bool emailVerified = false;
                bool phoneVerified = false;
                int userId = -1; // Initialize with a default value

                // If duplicate found, check verification status
                if (dbDataList != null && dbDataList.Count > 0)
                {
                    List<object[]> dbData = dbDataList[0];

                    if (dbData.Count > 0)
                    {
                        int emailVerifiedInt, phoneVerifiedInt;
                        if (int.TryParse(dbData[0][9].ToString(), out emailVerifiedInt) && int.TryParse(dbData[0][8].ToString(), out phoneVerifiedInt))
                        {
                            emailVerified = Convert.ToBoolean(emailVerifiedInt);
                            phoneVerified = Convert.ToBoolean(phoneVerifiedInt);
                        }
                        userId = Convert.ToInt32(dbData[0][0]);
                        Console.WriteLine($" \n User_id {userId}");
                    }
                }

                // If both email and phone are verified, return duplicate credentials error
                if (emailVerified && phoneVerified)
                {
                    resData.rData["rMessage"] = "Duplicate Credentials";
                    resData.rData["rCode"] = 1;
                    return resData;
                }

                // Convert date_of_birth to yyyy-MM-dd format
                string dateOfBirthInput = req.addInfo["date_of_birth"].ToString();
                DateTime dateOfBirth = DateTime.ParseExact(dateOfBirthInput, "dd-MM-yyyy", null);
                string formattedDateOfBirth = dateOfBirth.ToString("yyyy-MM-dd");

                 int age = DateTime.Today.Year - dateOfBirth.Year;
                if (dateOfBirth > DateTime.Today.AddYears(-age)) age--;

                // Insert or update user details
                MySqlParameter[] insertParams = new MySqlParameter[]
                {
                    new MySqlParameter("@full_name", req.addInfo["full_name"].ToString()),
                    new MySqlParameter("@phone_number", req.addInfo["phone_number"].ToString()),
                    new MySqlParameter("@email", req.addInfo["email"].ToString()),
                    new MySqlParameter("@date_of_birth", formattedDateOfBirth),
                     new MySqlParameter("@age", age),
                    // new MySqlParameter("@date_of_birth", req.addInfo["date_of_birth"].ToString()),
                    new MySqlParameter("@password", req.addInfo["password"].ToString()),
                    new MySqlParameter("@phone_verified", false),
                    new MySqlParameter("@email_verified", false)
                };

                var sq = @"INSERT INTO pc_student.TravelMates_Users 
                    (full_name, email, phone_number, date_of_birth, age, password, phone_verified, email_verified) 
                    VALUES (@full_name, @email, @phone_number, @date_of_birth, @age, @password, @phone_verified, @email_verified)
                    ON DUPLICATE KEY UPDATE
                    full_name=@full_name, date_of_birth=@date_of_birth, age=@age,password=@password, phone_verified=false, email_verified=false";

                var insertResult = ds.ExecuteInsertAndGetLastId(sq, insertParams);


                if (insertResult == null)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Registration Unsuccessful";
                }
                else
                {
                    if (userId == -1)
                    {
                        userId = Convert.ToInt32(insertResult);
                    }
                    // Generate OTP for phone and email
                    string phoneOtp = GenerateOTP();
                    string emailOtp = GenerateOTP();

                    // Send OTP to phone and email (async)
                    await SendOTPToPhone(req.addInfo["phone_number"].ToString(), phoneOtp);
                    await SendOTPToEmail(req.addInfo["email"].ToString(), emailOtp, req.addInfo["full_name"].ToString());
                    // await SendOTPToEmail(req.addInfo["email"].ToString(), emailOtp);

                    // Store OTP in respective tables
                    int phoneotp_id = StorePhoneOTP(req.addInfo["phone_number"].ToString(), phoneOtp);
                    int emailotp_id = StoreEmailOTP(req.addInfo["email"].ToString(), emailOtp);

                    // Prepare response data
                    resData.rData["rCode"] = 0;
                    resData.rData["rMessage"] = "Registration Successful. Please verify your phone and email.";
                    resData.rData["user_id"] = userId;
                    resData.rData["phoneotp_id"] = phoneotp_id;
                    resData.rData["emailotp_id"] = emailotp_id;
                }
            }
            catch (Exception ex)
            {
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
                // Log the exception
            }
            return resData;
        }


        private string GenerateOTP()
        {
            Random rand = new Random();
            return rand.Next(100000, 999999).ToString();
        }

        private async Task SendOTPToPhone(string phoneNumber, string phoneOtp)
        {
            // Logic to send OTP to phone (Example: Using Twilio)
            // Replace this with actual implementation
            Console.WriteLine($"Sending OTP {phoneOtp} to {phoneNumber}");
            try
            {
                // Format the phone number to E.164 format
                if (!phoneNumber.StartsWith("+"))
                {
                    // Assuming the phone number is for India, add the country code
                    phoneNumber = "+91" + phoneNumber;
                }

                Console.WriteLine("Formatted phone number: " + phoneNumber);

                var client = new RestClient("https://api.authkey.io");

                // Create the request with parameters
                var request = new RestRequest($"request?authkey=YourAuthKey" +
                                               $"&sms=Your OTP code is {phoneOtp}. This code is valid for 5 minutes." +
                                               $"&mobile={phoneNumber}" +
                                               $"&country_code=91" +
                                               $"&sender=SENDERID", Method.Get);

                // Execute the request
                var response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    Console.WriteLine("OTP SMS sent successfully to " + phoneNumber);
                }
                else
                {
                    Console.WriteLine("Failed to send OTP SMS: " + response.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending OTP SMS to " + phoneNumber + ": " + ex.Message);
                throw;  // Re-throw the exception after logging it
            }
        }

        private async Task SendOTPToEmail(string email, string emailOtp, string userName)
        // private async Task SendOTPToEmail(string email, string emailOtp)
        {

            Console.WriteLine($"Sending OTP {emailOtp} to {email}");
            try
            {
                var fromAddress = new MailAddress("jeevank028@gmail.com");
                var toAddress = new MailAddress(email);
                const string fromPassword = "dznk ezxs tfbc wfxb";
                const string subject = "OTP Verification Code for TravelMates ";
                // String userName = "TravelMates";


                // string body = $"Your OTP code is {emailOtp}. This code is valid for 5 minutes.";
                string body = $@"
                Dear {userName},

                Thank you for using TravelMates.

                Your One-Time Password (OTP) is {emailOtp}. This code is valid for 5 minutes. Please enter it to complete your verification process.

                If you did not request this code, please ignore this message.

                Thank you for your attention.

                Best regards,  
                The TravelMates Team
                ";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    await smtp.SendMailAsync(message);
                }

                Console.WriteLine("OTP email sent successfully to " + email);
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine("SMTP Exception: " + smtpEx.Message);
                if (smtpEx.InnerException != null)
                {
                    Console.WriteLine("Inner Exception: " + smtpEx.InnerException.Message);
                }
                throw;  // Re-throw the exception after logging it
            }
            catch (SocketException socketEx)
            {
                Console.WriteLine("Socket Exception: " + socketEx.Message);
                throw;  // Re-throw the exception after logging it
            }
            catch (Exception ex)
            {
                Console.WriteLine("An unexpected error occurred: " + ex.Message);
                throw;  // Re-throw the exception after logging it
            }
        }

        private int StorePhoneOTP(string phoneNumber, string phoneOtp)
        {
            MySqlParameter[] otpParams = new MySqlParameter[]
            {
                new MySqlParameter("@phone_number", phoneNumber),
                new MySqlParameter("@phone_otp", phoneOtp),
                new MySqlParameter("@expires_at", DateTime.Now.AddMinutes(5)) // OTP expires in 10 minutes
            };

            var insertQuery = @"
                INSERT INTO pc_student.TravelMates_PhoneVerification (phone_number, phone_otp, expires_at) 
                VALUES (@phone_number, @phone_otp, @expires_at) 
                ON DUPLICATE KEY UPDATE
                phone_otp = VALUES(phone_otp),
                expires_at = VALUES(expires_at)";
            var phoneotp_id = ds.ExecuteInsertAndGetLastId(insertQuery, otpParams);
            Console.WriteLine($"phoneotp_id {phoneotp_id}");
            return phoneotp_id;
        }

        private int StoreEmailOTP(string email, string emailOtp)
        {
            MySqlParameter[] otpParams = new MySqlParameter[]
            {
                new MySqlParameter("@email", email),
                new MySqlParameter("@email_otp", emailOtp),
                new MySqlParameter("@expires_at", DateTime.Now.AddMinutes(5)) // OTP expires in 10 minutes
            };

            var insertQuery = @"
                INSERT INTO pc_student.TravelMates_EmailVerification (email, email_otp, expires_at) 
                VALUES (@email, @email_otp, @expires_at) 
                ON DUPLICATE KEY UPDATE 
                email_otp= VALUES(email_otp),
                expires_at= VALUES(expires_at)";
            var emailotp_id = ds.ExecuteInsertAndGetLastId(insertQuery, otpParams);
            Console.WriteLine($"emailotp_id {emailotp_id}");
            return emailotp_id;
        }



        public async Task<responseData> VerifyAndCheckOTP(requestData req)
        {
            responseData resData = new responseData();
            try
            {
                int userId = -1;
                int emailOtpId = -1;
                int phoneOtpId = -1;
                string emailOtp = null;
                string phoneOtp = null;

                if (req.addInfo.TryGetValue("user_id", out object userIdObj) && ((JsonElement)userIdObj).TryGetInt32(out int uid))
                {
                    userId = uid;
                }

                if (req.addInfo.TryGetValue("emailotp_id", out object emailOtpIdObj) && ((JsonElement)emailOtpIdObj).TryGetInt32(out int eid))
                {
                    emailOtpId = eid;
                }

                if (req.addInfo.TryGetValue("phoneotp_id", out object phoneOtpIdObj) && ((JsonElement)phoneOtpIdObj).TryGetInt32(out int pid))
                {
                    phoneOtpId = pid;
                }

                if (req.addInfo.TryGetValue("email_otp", out object emailOtpObj) && ((JsonElement)emailOtpObj).ValueKind == JsonValueKind.String)
                {
                    emailOtp = ((JsonElement)emailOtpObj).GetString();
                }

                if (req.addInfo.TryGetValue("phone_otp", out object phoneOtpObj) && ((JsonElement)phoneOtpObj).ValueKind == JsonValueKind.String)
                {
                    phoneOtp = ((JsonElement)phoneOtpObj).GetString();
                }

                if (userId == -1)
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "User ID must be provided.";
                    return resData;
                }

                if (string.IsNullOrEmpty(emailOtp) && string.IsNullOrEmpty(phoneOtp))
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Either email OTP or phone OTP must be provided.";
                    return resData;
                }

                if (!string.IsNullOrEmpty(emailOtp) && emailOtpId != -1)
                {
                    var emailParams = new MySqlParameter[]
                    {
                new MySqlParameter("@otp", emailOtp),
                new MySqlParameter("@emailotp_id", emailOtpId),
                new MySqlParameter("@user_id", userId)
                    };
                    var emailQuery = @"SELECT * FROM pc_student.TravelMates_EmailVerification 
                               WHERE emailotp_id=@emailotp_id AND email_otp=@otp AND expires_at > NOW()";
                    var emailData = ds.executeSQL(emailQuery, emailParams);

                    if (emailData[0].Count > 0)
                    {
                        var updateEmailQuery = @"UPDATE pc_student.TravelMates_Users SET email_verified = TRUE WHERE user_id=@user_id";
                        ds.executeSQL(updateEmailQuery, emailParams);
                        resData.rData["email_verified"] = true;
                    }
                    else
                    {
                        resData.rData["rCode"] = 1;
                        resData.rData["rMessage"] = "Invalid or expired email OTP.";
                        return resData;
                    }
                }

                if (!string.IsNullOrEmpty(phoneOtp) && phoneOtpId != -1)
                {
                    var phoneParams = new MySqlParameter[]
                    {
                        new MySqlParameter("@otp", phoneOtp),
                        new MySqlParameter("@phoneotp_id", phoneOtpId),
                        new MySqlParameter("@user_id", userId)
                    };
                    var phoneQuery = @"SELECT * FROM pc_student.TravelMates_PhoneVerification 
                               WHERE phoneotp_id=@phoneotp_id AND phone_otp=@otp AND expires_at > NOW()";
                    var phoneData = ds.executeSQL(phoneQuery, phoneParams);

                    if (phoneData[0].Count > 0)
                    {
                        var updatePhoneQuery = @"UPDATE pc_student.TravelMates_Users SET phone_verified = TRUE WHERE user_id=@user_id";
                        ds.executeSQL(updatePhoneQuery, phoneParams);
                        resData.rData["phone_verified"] = true;
                    }
                    else
                    {
                        resData.rData["rCode"] = 1;
                        resData.rData["rMessage"] = "Invalid or expired phone OTP.";
                        return resData;
                    }
                }

                resData.rData["rCode"] = 0;
                resData.rData["rMessage"] = "SignUP successful.";
            }
            catch (Exception ex)
            {
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
                // Log the exception (you should have a logging mechanism here)
            }
            return resData;
        }


    }
}
