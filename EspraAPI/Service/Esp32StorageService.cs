using NHibernate;
using EspraAPI.Models;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate.Tool.hbm2ddl;

namespace EspraAPI.Service
{
    public class Esp32StorageService
    {
        private ISessionFactory sessionFactory;

        public Esp32StorageService()
        {
            sessionFactory = Fluently.Configure()
                .Database(MySQLConfiguration.Standard.ConnectionString("Server=localhost;Uid=username;Database=esp32snapshotdb;Pwd=password;"))
                .Mappings(i => i.FluentMappings.AddFromAssemblyOf<Program>())
                .ExposeConfiguration(cfg => new SchemaUpdate(cfg).Execute(true, true))
                .BuildSessionFactory();
        }

        public IList<Esp32Model> GetAll()
        {
            using NHibernate.ISession session = sessionFactory.OpenSession();
            using ITransaction transaction = session.BeginTransaction();
            return session.Query<Esp32Model>().ToList();
        }

        public async Task<bool> Add(Esp32Model model, CancellationToken token)
        {
            model.Base64SnapShot = $"data:image/jpeg;base64,{model.Base64SnapShot}";

            using NHibernate.ISession session = sessionFactory.OpenSession();
            using ITransaction transaction = session.BeginTransaction();
            await session.SaveOrUpdateAsync(model, cancellationToken: token);
            await session.FlushAsync(token);
            await transaction.CommitAsync(token);
            return true;
        }
    }
}
