﻿namespace EspraAPI.Identity
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set;} = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
