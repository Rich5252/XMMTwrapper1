using System.Xml.Linq;
using CredentialManagement;
using XMMTwrapper1;

public class CallsignData
{
    public string? Callsign { get; set; }
    public string? Name { get; set; }
    public string? City { get; set; }
    public string? Grid { get; set; }
    public string? Country { get; set; }
    public string? UScounty { get; set; }
    public string? USstate { get; set; }
}

public class QrzService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string BaseUrl = "https://xmldata.qrz.com/xml/current/";
    XNamespace ns = "http://xmldata.qrz.com";

    private string? _sessionKey;
    private string? _username;
    private string? _password;

    public void SetCredentials(bool bReset = false)
    {
        //login stored securely, retreive or enter for first usage
        // RightClick on main form "LookUp" to change/reset
        // uses NuGet "CredentialManagement" package

        CredentialVault _vault = new CredentialVault();
        var creds = _vault.Retrieve();

        if (bReset || creds == null)
        {
            //one time request here - use "Lookup" button RightClick to reset/change
            Form _frm = new frmSetCred(_vault);
            _frm.ShowDialog();
            creds = _vault.Retrieve();
        }

        if (creds != null)
        { 
            _username = creds.Value.user;
            _password = creds.Value.pass;
        }
    }

    public async Task<bool> EnsureLoggedInAsync()
    {
        if (!string.IsNullOrEmpty(_sessionKey)) return true;

        if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password)) return false; //ignore request if no login details
            //throw new InvalidOperationException("Credentials not set.");

        string url = $"{BaseUrl}?username={_username};password={_password};agent=DRL1.0";

        var response = await _httpClient.GetStringAsync(url);
        var doc = XDocument.Parse(response);

        var session = doc.Root?.Element(ns + "Session");

        string _error = session?.Element(ns + "Error")?.Value;
        _sessionKey = session?.Element(ns + "Key")?.Value;

        return !string.IsNullOrEmpty(_sessionKey);
    }

    public async Task<CallsignData?> GetCallsignDataAsync(string callsign)
    {
        if (string.IsNullOrEmpty(callsign)) return null;

        if (!await EnsureLoggedInAsync()) return null;

        string url = $"{BaseUrl}?s={_sessionKey};callsign={callsign}";
        var response = await _httpClient.GetStringAsync(url);
        var doc = XDocument.Parse(response);

        // Check if the session expired since our last use
        var error = doc.Root?.Element(ns + "Session")?.Element(ns + "Error")?.Value;
        if (error != null && (error.Contains("Session Timeout") || error.Contains("Invalid session")))
        {
            _sessionKey = null; // Clear the bad key
            return await GetCallsignDataAsync(callsign); // Recursive retry (will trigger login)
        }

        var data = doc.Root?.Element(ns + "Callsign");
        if (data == null) return null;

        //nickname or firstname
        string _name = $"{ data.Element(ns + "nickname")?.Value }".Trim();
        string _fname = $"{data.Element(ns + "fname")?.Value}".Trim();
        if (string.IsNullOrEmpty(_name)) _name = _fname;

        return new CallsignData
        {
            Callsign = data.Element(ns + "call")?.Value,
            Name = _name,
            City = data.Element(ns + "addr2")?.Value,
            Grid = data.Element(ns + "grid")?.Value,
            Country = data.Element(ns + "country")?.Value,
            UScounty = data.Element(ns + "county")?.Value,
            USstate = data.Element(ns + "state")?.Value
        };
    }
}



public class CredentialVault
{
    private const string AppIdentifier = "MyCWLogger_QRZ_Auth";

    public void Store(string user, string pass)
    {
        using var cred = new Credential
        {
            Target = AppIdentifier,
            Username = user,
            Password = pass,
            Type = CredentialType.Generic,
            PersistanceType = PersistanceType.LocalComputer
        };
        cred.Save();
    }

    public (string user, string pass)? Retrieve()
    {
        using var cred = new Credential { Target = AppIdentifier };
        return cred.Load() ? (cred.Username, cred.Password) : null;
    }
}