namespace bookShop.Service;

public class AuthService
{
    public bool Login(string username, string password)
    {
        return username == "admin" && password == "1234";
    }
}