namespace EspraAPI.Models
{
    public class Esp32Model
    {
        public virtual int Id { get; set; }

        public virtual string Base64SnapShot { get; set; } = string.Empty;

        public virtual string TimeStamp { get; set; } = string.Empty;

        public virtual bool IsValid => !string.IsNullOrEmpty(TimeStamp);
    }

}
