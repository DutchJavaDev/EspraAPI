using FluentNHibernate.Mapping;

namespace EspraAPI.Models
{
    public class Esp32ModelMapping : ClassMap<Esp32Model>
    {
        public Esp32ModelMapping()
        {
            Table("esp32_snapshots");
            Id(i => i.Id);
            Map(i => i.TimeStamp);
            Map(i => i.Base64SnapShot);
        }
    }
}
