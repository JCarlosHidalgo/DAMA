using Backend.DB.Daos.Concrete.Single.PaymentCredentials;

using MySql.Data.MySqlClient;

namespace Test.DB.Daos.Concrete.Single.PaymentCredentials;

[TestFixture]
public class TenantPaymentCredentialDaoTests
{
    [Test]
    public void Constructor_DoesNotThrow_WhenEntityIdentifierIsNamedId()
    {
        Assert.DoesNotThrow(() => new TenantPaymentCredentialDao(new MySqlConnection()));
    }
}
