using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors;

public sealed class PendingQrPaymentInjector : DataInjector
{
    public PendingQrPaymentInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/PendingQrPayment.csv' ")
        .Append("INTO TABLE PendingQrPayment ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("IGNORE 1 LINES ")
        .Append("(Id, TenantId, StudentId, TemplateId, ClassQuantity, Cost, Currency, QrImageUrl, CreatedAt, ExpiresAt)");
        _injectionCommand = sb.ToString();
    }
}
