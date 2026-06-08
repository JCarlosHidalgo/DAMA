using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors;

public sealed class SuccessSubscriptionPaymentInjector : DataInjector
{
    public SuccessSubscriptionPaymentInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/SuccessSubscriptionPayment.csv' ")
        .Append("INTO TABLE SuccessSubscriptionPayment ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("IGNORE 1 LINES ")
        .Append("(Id, TenantId, Level, Cost, Currency, PaidAt)");
        _injectionCommand = sb.ToString();
    }
}
