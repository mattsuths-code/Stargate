using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarGate.Server.Data
{
    [Table("StarGateLog")]
    public class StargateLog
    {
        public int Id { get; set; }
        public string LogLevel { get; set; }= string.Empty;
        public string Message { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
    }

    public class StargateLogConfiguration : IEntityTypeConfiguration<StargateLog>
    {
        public void Configure(EntityTypeBuilder<StargateLog> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
        }
    }
}
