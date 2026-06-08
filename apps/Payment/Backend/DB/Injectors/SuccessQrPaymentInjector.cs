using System.Text;

using SQLDaosPackage.Injectors;

namespace Backend.DB.Injectors;

public sealed class SuccessQrPaymentInjector : DataInjector
{
    public SuccessQrPaymentInjector()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("LOAD DATA INFILE '/var/lib/mysql-files/SuccessQrPayment.csv' ")
        .Append("INTO TABLE SuccessQrPayment ")
        .Append("FIELDS TERMINATED BY ',' ")
        .Append("IGNORE 1 LINES ")
        .Append("(Id, TenantId, StudentId, ClassQuantity, Cost, Currency, PaidAt)");
        _injectionCommand = sb.ToString();
    }
}
