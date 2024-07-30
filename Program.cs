
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using COMMON_PROJECT_STRUCTURE_API.services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using TravelMate_Api.services;

WebHost.CreateDefaultBuilder().
ConfigureServices(s =>
{
    IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
    s.AddSingleton<login>();
    s.AddSingleton<TravelMates_Users>();
    s.AddSingleton<TravelMates_SignIn>();
    s.AddSingleton<TravelMates_UserProfiles>();


   
    s.AddSingleton<upload>();
    s.AddSingleton<contact>();
   

    s.AddAuthorization();
    s.AddControllers();
    s.AddCors();
    s.AddAuthentication("SourceJWT").AddScheme<SourceJwtAuthenticationSchemeOptions, SourceJwtAuthenticationHandler>("SourceJWT", options =>
        {
            options.SecretKey = appsettings["jwt_config:Key"].ToString();
            options.ValidIssuer = appsettings["jwt_config:Issuer"].ToString();
            options.ValidAudience = appsettings["jwt_config:Audience"].ToString();
            options.Subject = appsettings["jwt_config:Subject"].ToString();
        });
}).Configure(app =>
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseCors(options =>
            options.WithOrigins("https://localhost:5002", "http://localhost:5001")
            .AllowAnyHeader().AllowAnyMethod().AllowCredentials());
    app.UseRouting();
    app.UseStaticFiles();

    app.UseEndpoints(e =>
    {
        var login = e.ServiceProvider.GetRequiredService<login>();
        var TravelMates_Users = e.ServiceProvider.GetRequiredService<TravelMates_Users>();
        var TravelMates_SignIn = e.ServiceProvider.GetRequiredService<TravelMates_SignIn>();
        var TravelMates_UserProfiles = e.ServiceProvider.GetRequiredService<TravelMates_UserProfiles>();
       
 
        var upload = e.ServiceProvider.GetRequiredService<upload>();
        var contact = e.ServiceProvider.GetRequiredService<contact>();


        e.MapPost("login",
     [AllowAnonymous] async (HttpContext http) =>
     {
         var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
         requestData rData = JsonSerializer.Deserialize<requestData>(body);
         if (rData.eventID == "1001") // update
             await http.Response.WriteAsJsonAsync(await login.Login(rData));

     });

        e.MapPost("TravelMates_Users",
     [AllowAnonymous] async (HttpContext http) =>
     {
         var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
         requestData rData = JsonSerializer.Deserialize<requestData>(body);
         if (rData.eventID == "1001") // TravelMates_UserSignUp
             await http.Response.WriteAsJsonAsync(await TravelMates_Users.TravelMates_UserSignUp(rData));
         if (rData.eventID == "1002") // VerifyAndCheckOTP
             await http.Response.WriteAsJsonAsync(await TravelMates_Users.VerifyAndCheckOTP(rData));


     });


       e.MapPost("TravelMates_SignIn",
     [AllowAnonymous] async (HttpContext http) =>
     {
         var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
         requestData rData = JsonSerializer.Deserialize<requestData>(body);
         if (rData.eventID == "1001") // UserSignIn_UsingPassword
             await http.Response.WriteAsJsonAsync(await TravelMates_SignIn.UserSignIn_UsingPassword(rData));
         if (rData.eventID == "1002") // GenerateOtpForLogin
             await http.Response.WriteAsJsonAsync(await TravelMates_SignIn.GenerateOtpForLogin(rData));
        if (rData.eventID == "1003") // VerifyOtpForLogin
             await http.Response.WriteAsJsonAsync(await TravelMates_SignIn.VerifyOtpForLogin(rData));
         if (rData.eventID == "1004") // ForgotPassword
             await http.Response.WriteAsJsonAsync(await TravelMates_SignIn.ForgotPassword(rData));
         if (rData.eventID == "1005") // VerifyOtpForForgotPassword
             await http.Response.WriteAsJsonAsync(await TravelMates_SignIn.VerifyOtpForForgotPassword(rData));
         if (rData.eventID == "1006") // ResetPassword
             await http.Response.WriteAsJsonAsync(await TravelMates_SignIn.ResetPassword(rData));


     });

     e.MapPost("TravelMates_UserProfiles",
     [AllowAnonymous] async (HttpContext http) =>
     {
         var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
         requestData rData = JsonSerializer.Deserialize<requestData>(body);
         if (rData.eventID == "1001") // UpdateUserProfileImage
             await http.Response.WriteAsJsonAsync(await TravelMates_UserProfiles.UpdateUserProfileImage(rData));
         if (rData.eventID == "1002") // DeleteProfile
             await http.Response.WriteAsJsonAsync(await TravelMates_UserProfiles.DeleteProfile(rData));
        if (rData.eventID == "1003") // VerifyOtpForLogin
             await http.Response.WriteAsJsonAsync(await TravelMates_UserProfiles.ReadProfile(rData));
         if (rData.eventID == "1004") // GetRandomUserProfiles
             await http.Response.WriteAsJsonAsync(await TravelMates_UserProfiles.GetRandomUserProfiles(rData));
         if (rData.eventID == "1005") // UpdateProfile
             await http.Response.WriteAsJsonAsync(await TravelMates_UserProfiles.UpdateProfile(rData));
         if (rData.eventID == "1006") // DeleteProfile
             await http.Response.WriteAsJsonAsync(await TravelMates_UserProfiles.DeleteProfile(rData));
         if (rData.eventID == "1007") // UpdateProfile
             await http.Response.WriteAsJsonAsync(await TravelMates_UserProfiles.GetUserProfile(rData));
         if (rData.eventID == "1008") // DeleteProfile
             await http.Response.WriteAsJsonAsync(await TravelMates_UserProfiles.UpdateUserProfile(rData));


     });


   
       

        e.MapPost("upload",
  [AllowAnonymous] async (HttpContext http) =>
  {
      var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
      requestData rData = JsonSerializer.Deserialize<requestData>(body);
      if (rData.eventID == "1001") // update
          await http.Response.WriteAsJsonAsync(await upload.Upload(rData));

  });

        e.MapPost("contact",
   [AllowAnonymous] async (HttpContext http) =>
   {
       var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
       requestData rData = JsonSerializer.Deserialize<requestData>(body);
       if (rData.eventID == "1005") // update
           await http.Response.WriteAsJsonAsync(await contact.Contact(rData));

   });

        IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        e.MapGet("/dbstring",
                  async c =>
                  {
                      dbServices dspoly = new dbServices();
                      await c.Response.WriteAsJsonAsync("{'mongoDatabase':" + appsettings["mongodb:connStr"] + "," + " " + "MYSQLDatabase" + " =>" + appsettings["db:connStrPrimary"]);
                  });

        e.MapGet("/bing",
          async c => await c.Response.WriteAsJsonAsync("{'Name':'Anish','Age':'26','Project':'COMMON_PROJECT_STRUCTURE_API'}"));
    });
}).Build().Run();
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
public record requestData
{
    [Required]
    public string eventID { get; set; }
    [Required]
    public IDictionary<string, object> addInfo { get; set; }
}

public record responseData
{
    public responseData()
    {
        eventID = "";
        rStatus = 0;
        rData = new Dictionary<string, object>();
    }
    [Required]
    public int rStatus { get; set; } = 0;
    public string eventID { get; set; }
    public IDictionary<string, object> addInfo { get; set; }
    public IDictionary<string, object> rData { get; set; }
}
