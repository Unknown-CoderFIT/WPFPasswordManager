using System;

namespace PasswordManager.Models;

public class Credential
{
    public string Id { get; set; }
    public string ServiceName { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
    public string Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
