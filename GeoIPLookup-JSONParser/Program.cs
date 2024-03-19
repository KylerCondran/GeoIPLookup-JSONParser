using Newtonsoft.Json;
using System.Data;
using System.Net;
using System.Data.SqlClient;

string connectionString = "SQLConnectionStringGoesHere";
string storedProcedure = "StoredProcedure_TitleGoesHere";
string GeoIPLookupAPI = "http://ip-api.com/json/";
/* 
========================================================= 
Example Table Schema
=========================================================
USE [Database]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Table](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[IPAddress] [nvarchar](100) NOT NULL,
	[City] [nvarchar](100) NULL,
	[Country] [nvarchar](50) NULL,
	[CountryCode] [nvarchar](10) NULL,
	[ISP] [nvarchar](100) NULL,
	[Latitude] [decimal](18, 4) NULL,
	[Longitude] [decimal](18, 4) NULL,
	[Organization] [nvarchar](100) NULL,
	[Region] [nvarchar](10) NULL,
	[RegionName] [nvarchar](100) NULL,
	[TimeZone] [nvarchar](100) NULL,
	[ZipCode] [nvarchar](10) NULL,
	[AutonomousSystem] [nvarchar](100) NULL,
 CONSTRAINT [PK_Table] PRIMARY KEY CLUSTERED 
(
	[IPAddress] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
=========================================================
Example Stored Procedure
=========================================================
USE [Database]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [StoredProcedure_TitleGoesHere] 
       @IPAddress                     NVARCHAR(100)  = NULL, 
       @City                          NVARCHAR(100)  = NULL,
	   @Country                       NVARCHAR(50)  = NULL,
	   @CountryCode                   NVARCHAR(10)  = NULL,
	   @ISP                           NVARCHAR(100)  = NULL,
	   @Latitude                      DECIMAL(18, 4)  = NULL,
	   @Longitude                     DECIMAL(18, 4)  = NULL,
	   @Organization                  NVARCHAR(100)  = NULL,
	   @Region                        NVARCHAR(10)  = NULL,
	   @RegionName                    NVARCHAR(100)  = NULL,
	   @TimeZone                      NVARCHAR(100)  = NULL,
	   @ZipCode                       NVARCHAR(10)  = NULL,
	   @AutonomousSystem              NVARCHAR(100)  = NULL
AS 
BEGIN 
SET NOCOUNT ON 
IF EXISTS (SELECT IPAddress FROM [Database].[Table] WHERE IPAddress = @IPAddress)
	UPDATE [Database].[Table] SET City = @City, Country = @Country, CountryCode = @CountryCode, ISP = @ISP, Latitude = @Latitude, Longitude = @Longitude, Organization = @Organization, Region = @Region, RegionName = @RegionName, TimeZone = @TimeZone, ZipCode = @ZipCode, AutonomousSystem = @AutonomousSystem
	WHERE IPAddress = @IPAddress       
END
 */
try
{
    //Select existing IPAddresses from the SQL table with no Geo IP information. We take 45 At a time to avoid overloading the request limit per 5 minutes on the api.
    using SqlConnection conn = new(connectionString);
    conn.Open();
    using SqlCommand comm = new("SELECT TOP 45 IPAddress FROM [Database].[Table] WHERE Country IS NULL ORDER BY ID DESC", conn);
    using SqlDataReader RDR = comm.ExecuteReader();
    //Iterate over each one returned.
    while (RDR.Read())
    {
        try
        {
            string theIPAddress = RDR.GetString(0);
            using (WebClient wc = new WebClient())
            {
                //Make a request to the GeoIP Lookup API with the IPAddress returned from the SQL Table.
                var json = wc.DownloadString(GeoIPLookupAPI + theIPAddress);
                //Take the result and pass it to a stored procedure to enter the new information back into the SQL table.
                Item myObject = JsonConvert.DeserializeObject<Item>(json);
                try
                {
                    using SqlConnection conn2 = new(connectionString);
                    using SqlCommand comm2 = new();
                    comm2.Connection = conn2;
                    comm2.CommandType = CommandType.StoredProcedure;
                    comm2.CommandText = storedProcedure;
                    comm2.Parameters.AddWithValue("@IPAddress", theIPAddress);
                    comm2.Parameters.AddWithValue("@City", myObject.city);
                    comm2.Parameters.AddWithValue("@Country", myObject.country);
                    comm2.Parameters.AddWithValue("@CountryCode", myObject.countryCode);
                    comm2.Parameters.AddWithValue("@ISP", myObject.isp);
                    comm2.Parameters.AddWithValue("@Latitude", myObject.lat);
                    comm2.Parameters.AddWithValue("@Longitude", myObject.lon);
                    comm2.Parameters.AddWithValue("@Organization", myObject.org);
                    comm2.Parameters.AddWithValue("@Region", myObject.region);
                    comm2.Parameters.AddWithValue("@RegionName", myObject.regionName);
                    comm2.Parameters.AddWithValue("@TimeZone", myObject.timezone);
                    comm2.Parameters.AddWithValue("@ZipCode", myObject.zip);
                    comm2.Parameters.AddWithValue("@AutonomousSystem", myObject.myas);
                    conn2.Open();
                    comm2.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    conn.Close();
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}

//Class to hold our JSON object returned from the API
public class Item
{
    [JsonProperty("status")]
    public string status { get; set; }
    [JsonProperty("country")]
    public string country { get; set; }
    [JsonProperty("countryCode")]
    public string countryCode { get; set; }
    [JsonProperty("region")]
    public string region { get; set; }
    [JsonProperty("regionName")]
    public string regionName { get; set; }
    [JsonProperty("city")]
    public string city { get; set; }
    [JsonProperty("zip")]
    public string zip { get; set; }
    [JsonProperty("lat")]
    public decimal lat { get; set; }
    [JsonProperty("lon")]
    public decimal lon { get; set; }
    [JsonProperty("timezone")]
    public string timezone { get; set; }
    [JsonProperty("isp")]
    public string isp { get; set; }
    [JsonProperty("org")]
    public string org { get; set; }
    [JsonProperty("as")]
    public string myas { get; set; }
    [JsonProperty("query")]
    public string query { get; set; }
}