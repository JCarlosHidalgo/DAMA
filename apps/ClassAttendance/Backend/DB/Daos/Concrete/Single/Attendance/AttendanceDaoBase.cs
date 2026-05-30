using System.Text;

using SQLDaosPackage.Daos.MySQL;
using SQLDaosPackage.Entities;

namespace Backend.DB.Daos.Concrete.Single.Attendance;

public abstract class AttendanceDaoBase<TAttendance> : MySQLThreeForeignDao<TAttendance>
    where TAttendance : class, IThreeForeignEntity, new()
{
    protected abstract TAttendance BuildFromCurrentRow();

    protected sealed override TAttendance MapReaderToEntity()
    {
        _entity = BuildFromCurrentRow();
        return _entity;
    }

    protected sealed override List<TAttendance> MapReaderToEntitiesList()
    {
        _entitiesList = new List<TAttendance>();
        while (_mySqlReader!.Read())
        {
            _entity = BuildFromCurrentRow();
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    protected sealed override StringBuilder CreateCommandIntoStringBuilder(TAttendance attendance)
    {
        throw new NotSupportedException(
            $"{GetType().Name} does not support generic create — use the specific stored procedure or insert method.");
    }

    protected sealed override StringBuilder UpdateCommandIntoStringBuilder(TAttendance attendance)
    {
        throw new NotSupportedException(
            $"{GetType().Name} does not support generic update — use the specific stored procedure or insert method.");
    }

    public sealed override TAttendance? Read(Guid id1, Guid id2, Guid id3)
    {
        throw new NotSupportedException(
            $"{GetType().Name} does not support generic read by three foreign keys — the real primary key includes ClassDate. Use the specific stored-procedure method.");
    }

    public sealed override Task<TAttendance?> ReadAsync(Guid id1, Guid id2, Guid id3)
    {
        throw new NotSupportedException(
            $"{GetType().Name} does not support generic read by three foreign keys — the real primary key includes ClassDate. Use the specific stored-procedure method.");
    }

    public sealed override bool Delete(Guid id1, Guid id2, Guid id3)
    {
        throw new NotSupportedException(
            $"{GetType().Name} does not support generic delete by three foreign keys — use DeleteByClassForTenantAsync or a specific stored procedure.");
    }

    public sealed override Task<bool> DeleteAsync(Guid id1, Guid id2, Guid id3)
    {
        throw new NotSupportedException(
            $"{GetType().Name} does not support generic delete by three foreign keys — use DeleteByClassForTenantAsync or a specific stored procedure.");
    }
}
