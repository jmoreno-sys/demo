namespace System.Security.Claims
{
    public static class CustomClaimTypes
    {
        /// <summary>
        /// Representa la pantalla asignada al rol
        /// </summary>
        public const string Screen = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/screen";

        /// <summary>
        /// Representa el codigo del contratista solo para usuarios tipo contratistas
        /// </summary>
        public const string Owner = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/owner";

        /// <summary>
        /// Representa el tipo de usuario, si es contratista u otro
        /// </summary>
        public const string OwnerType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/ownertype";
    }
}
