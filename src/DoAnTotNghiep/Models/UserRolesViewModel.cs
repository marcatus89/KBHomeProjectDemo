    using System.Collections.Generic;

    namespace DoAnTotNghiep.Models
    {
        public class UserRolesViewModel
        {
            public string UserId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public List<RoleViewModel> Roles { get; set; } = new List<RoleViewModel>();
        }

        public class RoleViewModel
        {
            public string RoleName { get; set; } = string.Empty;
            public bool IsSelected { get; set; }
        }
    }
    
