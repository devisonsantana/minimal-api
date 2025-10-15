using minimal_api.Domain.Entities;

namespace minimal_api.Domain.ModelViews
{
    public record UserModelView
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public UserModelView() { }
        public UserModelView(User user)
        {
            Id = user.Id;
            Email = user.Email;
            Role = user.Role;
        }
    }
}