using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text.Json;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

public class decryptService
{

    IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
    
    //private readonly string SERVER_PRIVATE_KEY_UTI;



    


    public string getHash(string stringToCompute)
    {
        SHA256 mySHA256 = SHA256.Create();
        // convert string to byte array
        byte[] strToComputeBytes=Encoding.UTF8.GetBytes(stringToCompute);
        byte[] byteHash = mySHA256.ComputeHash(strToComputeBytes);
        return BitConverter.ToString(byteHash).Replace("-","").ToLower();
    }

    // public string AESDecrypt(string base64Key, string base64Ciphertext)
    // {
    //     // convert from base64 to raw bytes spans
    //     var encryptedData = Convert.FromBase64String(base64Ciphertext).AsSpan();
    //     var key = Convert.FromBase64String(base64Key).AsSpan();

    //     var tagSizeBytes = 16; // 128 bit encryption / 8 bit = 16 bytes
    //     var ivSizeBytes = 12; // 12 bytes iv
    
    //     // ciphertext size is whole data - iv - tag
    //     var cipherSize = encryptedData.Length - tagSizeBytes - ivSizeBytes;

    //     // extract iv (nonce) 12 bytes prefix
    //     var iv = encryptedData.Slice(0, ivSizeBytes);
    
    //     // followed by the real ciphertext
    //     var cipherBytes = encryptedData.Slice(ivSizeBytes, cipherSize);

    //     // followed by the tag (trailer)
    //     var tagStart = ivSizeBytes + cipherSize;
    //     var tag = encryptedData.Slice(tagStart);

    //     // now that we have all the parts, the decryption
    //     Span<byte> plainBytes = cipherSize < 1024
    //         ? stackalloc byte[cipherSize]
    //         : new byte[cipherSize];

    //     //var ae= new AesCng();    
    //     var aes = new AesGcm(key);
        

    //     aes.Decrypt(iv, cipherBytes, tag, plainBytes);
    //     return Encoding.UTF8.GetString(plainBytes);
    // }
public string AESDecrypt(string base64Key, string base64Ciphertext)
{
    var fullCipher = Convert.FromBase64String(base64Ciphertext);

    var iv = new byte[16];
    var cipher = new byte[fullCipher.Length - iv.Length];

    Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
    Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

    var key = Encoding.UTF8.GetBytes(base64Key);

    using (var aesAlg = Aes.Create())
    {
        aesAlg.Key = key;
        aesAlg.IV = iv;
        aesAlg.Mode = CipherMode.CBC;
        aesAlg.Padding = PaddingMode.PKCS7;

        using (var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
        {
            using (var msDecrypt = new MemoryStream(cipher))
            {
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }
}
 
public string AESDecrypts(string base64Key, string base64Ciphertext)
{
    var fullCipher = Convert.FromBase64String(base64Ciphertext);

    var iv = new byte[16];
    var cipher = new byte[fullCipher.Length - iv.Length];

    Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
    Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

    var key = Encoding.UTF8.GetBytes(base64Key);

    using (var aesAlg = Aes.Create())
    {
        aesAlg.Key = key;
        aesAlg.IV = iv;
        aesAlg.Mode = CipherMode.CBC;
        aesAlg.Padding = PaddingMode.None;

        using (var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
        {
            using (var msDecrypt = new MemoryStream(cipher))
            {
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        csDecrypt.CopyTo(memoryStream);
                        byte[] decryptedBytes = memoryStream.ToArray();
                        
                        // Convert the bytes to a string using an appropriate encoding
                        // You might need to try different encodings depending on your data
                        string result = Encoding.UTF8.GetString(decryptedBytes);
                        
                        return result;
                    }
                }
            }
        }
    }
}



    // this function is called only once copy the publoc and private key from the console to be shared
    public  void GeneratePrivatePublicKeyPair() 
    {
        var name = "test";
        //var privateKeyXmlFile = name + "_priv.xml";
        //var publicKeyXmlFile = name + "_pub.xml";
        //var publicKeyFile = name + ".pub";

        using var provider = new RSACryptoServiceProvider(1024);
        Console.Write(provider.ToXmlString(true)); // private key
        Console.Write(provider.ToXmlString(false)); // public key
        //var x = provider.ImportRSAPrivateKey();
        //provider.ToString(true)
        //File.WriteAllText(privateKeyXmlFile, provider.ToXmlString(true));
        //File.WriteAllText(publicKeyXmlFile, provider.ToXmlString(false));
        //using var publicKeyWriter = File.CreateText(publicKeyFile);
        //ExportPublicKey(provider, publicKeyWriter);
        var x=1;
    }

    public void testCrypto()
    {
      //lets take a new CSP with a new 2048 bit rsa key pair
      var csp = new RSACryptoServiceProvider(2048);

      //how to get the private key
      var privKey = csp.ExportParameters(true);

      //and the public key ...
      var pubKey = csp.ExportParameters(false);

      //converting the public key into a string representation
      string pubKeyString;
      {
        //we need some buffer
        var sw = new System.IO.StringWriter();
        //we need a serializer
        var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
        //serialize the key into the stream
        xs.Serialize(sw, pubKey);
        //get the string from the stream
        pubKeyString = sw.ToString();
      }
      //csp.ExportRSAPublicKey()
   
      //converting it back
      {
        //get a stream from the string
        var sr = new System.IO.StringReader(pubKeyString);
        //we need a deserializer
        var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
        //get the object back from the stream
        pubKey = (RSAParameters)xs.Deserialize(sr);
      }

      //conversion for the private key is no black magic either ... omitted

      //we have a public key ... let's get a new csp and load that key
      csp = new RSACryptoServiceProvider();
      csp.ImportParameters(pubKey);

      //we need some data to encrypt
      var plainTextData = "foobar";

      //for encryption, always handle bytes...
      var bytesPlainTextData = System.Text.Encoding.Unicode.GetBytes(plainTextData);

      //apply pkcs#1.5 padding and encrypt our data 
      var bytesCypherText = csp.Encrypt(bytesPlainTextData, false);

      //we might want a string representation of our cypher text... base64 will do
      var cypherText = Convert.ToBase64String(bytesCypherText);


      /*
       * some transmission / storage / retrieval
       * 
       * and we want to decrypt our cypherText
       */

      //first, get our bytes back from the base64 string ...
      bytesCypherText = Convert.FromBase64String(cypherText);

      //we want to decrypt, therefore we need a csp and load our private key
      csp = new RSACryptoServiceProvider();
      csp.ImportParameters(privKey);

      //decrypt and strip pkcs#1.5 padding
      bytesPlainTextData = csp.Decrypt(bytesCypherText, false);

      //get our original plainText back...
      plainTextData = System.Text.Encoding.Unicode.GetString(bytesPlainTextData);
    }


    private static void DecryptPrivate(string dataToDecrypt)
    {
        var name = "test";
        var encryptedBase64 = @"Rzabx5380rkx2+KKB+HaJP2dOXDcOC7SkYOy4HN8+Nb9HmjqeZfGQlf+ZUa6uAfAJ3oAB2iIlHlnx+iXK3XDIX3izjoW1eeiNmdOWieNCu6YXqW4denUVEv0Z4EpAmEYgVImnEzoMdmPDEcl9UHgdWUmS4Bnq6T8Yqh3UZ/4NOc=";
        var encrypted = Convert.FromBase64String(encryptedBase64);
        using var privateKey = new RSACryptoServiceProvider();
        privateKey.FromXmlString(File.ReadAllText(name + "_priv.xml"));
        var decryptedBytes = privateKey.Decrypt(encrypted, false);
        var dectryptedText = Encoding.UTF8.GetString(decryptedBytes);
    }
    public string EncryptAES(string aesKey, string data)
    {
        var key = Encoding.UTF8.GetBytes(aesKey);//16 bit or 32 bit key string

        using (var aesAlg = Aes.Create())
        {
            using (var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV))
            {
            
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(data);
                    }

                    var iv = aesAlg.IV;

                    var decryptedContent = msEncrypt.ToArray();

                    var result = new byte[iv.Length + decryptedContent.Length];

                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                    Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

                    return Convert.ToBase64String(result);
                }
            }
        }
    }

        // public static string EncryptWithPrivate(string privateKeyB64, string data)
        // {
        //     // byte[] privateKey = Convert.FromBase64String(privateKeyB64);
        //     // using (var rsa = RSA.Create())
        //     // {
        //     //     rsa.ImportRSAPrivateKey(privateKey, out _);

        //     //     byte[] encryptedData = rsa.Encrypt(Encoding.UTF8.GetBytes(data), RSAEncryptionPadding.Pkcs1);
        //     //     return Convert.ToBase64String(encryptedData);
        //     // }

        //     //string a = "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JSUV2Z0lCQURBTkJna3Foa2lHOXcwQkFRRUZBQVNDQktnd2dnU2tBZ0VBQW9JQkFRREpPTWRTYzVKT3hiMGMKTGViWWphT1VqLyt1UzJXZT0tLS0tRU5EIFBSSVZBVEUgS0VZLS0tLS0K";

        //     //string base64Encoded = Base64Encode(privateKeyB64);
        // // Console.WriteLine(base64Encoded);
        //    // return Convert.ToBase64String(base64Encoded.ToString());
        // }
        public static string Base64Encode(string plainText, string data)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            string base64Encoded = Convert.ToBase64String(plainTextBytes);
            return base64Encoded;
        }
    private static void EncryptPrivate(string dataToDecrypt)
    {
        var name = "test";
        var encryptedBase64 = @"Rzabx5380rkx2+KKB+HaJP2dOXDcOC7SkYOy4HN8+Nb9HmjqeZfGQlf+ZUa6uAfAJ3oAB2iIlHlnx+iXK3XDIX3izjoW1eeiNmdOWieNCu6YXqW4denUVEv0Z4EpAmEYgVImnEzoMdmPDEcl9UHgdWUmS4Bnq6T8Yqh3UZ/4NOc=";
        var encrypted = Convert.FromBase64String(encryptedBase64);
        using var privateKey = new RSACryptoServiceProvider();
        privateKey.FromXmlString(File.ReadAllText(name + "_priv.xml"));
        var decryptedBytes = privateKey.Decrypt(encrypted, false);
        var dectryptedText = Encoding.UTF8.GetString(decryptedBytes);
    }
    //     public   string EncryptUsingPublicKey(string data, string publicKeyBase64) 
    //     {  

    //  try
    //         { 

    //             // Convert the Base64 string to bytes
    //             byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);

    //             // Create an instance of RSA with a 4096-bit public key
    //             using (RSA rsa = RSA.Create())
    //             {
    //                 // Import the public key
    //                 rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

    //                 // Data to encrypt
    //                 string dataToEncrypt = data;
    //                 byte[] dataBytes = Encoding.UTF8.GetBytes(dataToEncrypt);

    //                 // Encrypt the data using RSA encryption
    //                 byte[] encryptedBytes = rsa.Encrypt(dataBytes, RSAEncryptionPadding.Pkcs1);

    //                 // Convert the encrypted bytes to Base64 or any other suitable format
    //                 string encryptedData = Convert.ToBase64String(encryptedBytes);

    //                 Console.WriteLine("Encrypted Data:");
    //                 Console.WriteLine(encryptedData);
    //                 return encryptedData;
    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             Console.WriteLine("Error: " + ex.Message);
    //             return ex.Message;
    //         }
    //     } 
    //     public   string DecryptUsingPrivateKey(string encryptedData, string privateKeyBase64) 
    //     {  

    //  try
    //         { 

    //             byte[] privateKeyBytes = Convert.FromBase64String(privateKeyBase64);

    //             using (RSA rsa = RSA.Create())
    //             {
    //                 // Import the private key
    //                 rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);


    //                 // Convert the Base64 string to bytes
    //                 byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

    //                 // Decrypt the data using RSA decryption
    //                 byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);

    //                 // Convert the decrypted bytes back to the original text
    //                 string decryptedData = Encoding.UTF8.GetString(decryptedBytes);

    //                 Console.WriteLine("Decrypted Data:");
    //                 Console.WriteLine(decryptedData);
    //                 return decryptedData;
    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             Console.WriteLine("Error: " + ex.Message);
    //             return ex.Message;
    //         }
    //     } 
    //     public   string EncryptUsingPrivateKey(string dataToEncrypt, string privateKeyBase64) {  

    //  try
    //         { 

    //             byte[] privateKeyBytes = Convert.FromBase64String(privateKeyBase64);

    //             using (RSA rsa = RSA.Create())
    //             {
    //                 // Import the private key
    //                 rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);


    //                 // Convert the Base64 string to bytes 
    //                 byte[] dataBytes = Encoding.UTF8.GetBytes(dataToEncrypt);

    //                  // Encrypt the data using RSA encryption
    //                     byte[] encryptedBytes = rsa.Encrypt(dataBytes, RSAEncryptionPadding.Pkcs1);

    //                 // Convert the encrypted bytes to Base64 or any other suitable format
    //                 string encryptedData = Convert.ToBase64String(encryptedBytes);

    //                 Console.WriteLine(encryptedData);
    //                 return encryptedData;
    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             Console.WriteLine("Error: " + ex.Message);
    //             return ex.Message;
    //         }
    //     } 
    //     public   string DecryptUsingPublicKey(string encryptedData, string publicKeyBase64) {  

    //  try
    //         { 

    //             byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);

    //             using (RSA rsa = RSA.Create())
    //             {
    //                 // Import the private key
    //                 rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);


    //                 // Convert the Base64 string to bytes
    //                 byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

    //                 // Decrypt the data using RSA decryption
    //                 byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);

    //                 // Convert the decrypted bytes back to the original text
    //                 string decryptedData = Encoding.UTF8.GetString(decryptedBytes);

    //                 Console.WriteLine("Decrypted Data:");
    //                 Console.WriteLine(decryptedData);
    //                 return decryptedData;
    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             Console.WriteLine("Error: " + ex.Message);
    //             return ex.Message;
    //         }
    //     } 
    public string encryptWithPublicKey(string plainText, string publicKey)
    {
        RSACryptoServiceProvider RSApublicKey = ImportPublicKey(publicKey);
        //for encryption, always handle bytes...
        var bytesPlainTextData = System.Text.Encoding.Unicode.GetBytes(plainText);

        //apply pkcs#1.5 padding and encrypt our data 
        var bytesCypherText = RSApublicKey.Encrypt(bytesPlainTextData, false);

        //we might want a string representation of our cypher text... base64 will do
        var cypherText = Convert.ToBase64String(bytesCypherText);
        return cypherText;

    }
    public string decryptWithPrivateKey(string cypherText, string privateKey)
    {

       try{ RSACryptoServiceProvider RSAprivateKey = ImportPrivateKey(privateKey);
        //first, get our bytes back from the base64 string ...
        var bytesCypherText = Convert.FromBase64String(cypherText);

        //we want to decrypt, therefore we need a csp and load our private key 

        //decrypt and strip pkcs#1.5 padding
        var bytesPlainTextData = RSAprivateKey.Decrypt(bytesCypherText, false);

        //get our original plainText back...
        var plainTextData = Encoding.UTF8.GetString(bytesPlainTextData);
        // var plainTextData = System.Text.Encoding.Unicode.GetString(bytesPlainTextData);
        return plainTextData;
        }
        catch(Exception ex)
        {

        }
        return null;

    }



    public static RSACryptoServiceProvider ImportPrivateKey(string pem)
    {
        PemReader pr = new PemReader(new StringReader(pem));
        AsymmetricCipherKeyPair KeyPair = (AsymmetricCipherKeyPair)pr.ReadObject();
        RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)KeyPair.Private);

        RSACryptoServiceProvider csp = new RSACryptoServiceProvider();// cspParams);
        csp.ImportParameters(rsaParams);
        return csp;
    }

    public static RSACryptoServiceProvider ImportPublicKey(string pem)
    {
        PemReader pr = new PemReader(new StringReader(pem));
        AsymmetricKeyParameter publicKey = (AsymmetricKeyParameter)pr.ReadObject();
        RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaKeyParameters)publicKey);

        RSACryptoServiceProvider csp = new RSACryptoServiceProvider();// cspParams);
        csp.ImportParameters(rsaParams);
        return csp;
    }
   
      public  string GetSha256Hash(string input){
        using (var hashAlgorithm = SHA256.Create()){
            var byteValue = Encoding.UTF8.GetBytes(input);
            var byteHash = hashAlgorithm.ComputeHash(byteValue);
            return Convert.ToBase64String(byteHash);
        }
    }
    
}
