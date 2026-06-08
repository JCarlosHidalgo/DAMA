using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors;

public sealed class FailedQrPaymentInjector : DataInjector
{
    public FailedQrPaymentInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/FailedQrPayment.csv' ")
        .Append("INTO TABLE FailedQrPayment ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("IGNORE 1 LINES ")
        .Append("(Id, TenantId, StudentId, ClassQuantity, Cost, Currency, FailedAt, FailureReason)");
        _injectionCommand = sb.ToString();
    }
}
