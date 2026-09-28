using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OSCU.Domain.Entities;

namespace OSCU.Persistence.Configurations;

public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.ToTable("referrals");

        builder.HasKey(referral => referral.Id);

        builder.Property(referral => referral.Id)
            .HasColumnName("id")
            // The application supplies a version 7 GUID; Postgres must not
            // overwrite it with a default.
            .ValueGeneratedNever();

        builder.Property(referral => referral.ReferralReference)
            .HasColumnName("referral_reference")
            .HasMaxLength(Referral.ReferralReferenceMaxLength)
            .IsRequired();

        builder.Property(referral => referral.Subject)
            .HasColumnName("subject")
            .HasMaxLength(Referral.SubjectMaxLength)
            .IsRequired();

        builder.Property(referral => referral.Description)
            .HasColumnName("description")
            .HasMaxLength(Referral.DescriptionMaxLength);

        builder.Property(referral => referral.Status)
            .HasColumnName("status")
            .HasMaxLength(Referral.StatusMaxLength)
            .IsRequired();

        // timestamptz, not timestamp. Storing local time without an offset in
        // a system that will run across BST and GMT is how you lose an hour
        // twice a year.
        builder.Property(referral => referral.ReceivedDate)
            .HasColumnName("received_date")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(referral => referral.CreatedDate)
            .HasColumnName("created_date")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // The business identifier must be unique. The service checks first to
        // return a clean 409, but this index is what actually guarantees it
        // under concurrent requests.
        builder.HasIndex(referral => referral.ReferralReference)
            .IsUnique()
            .HasDatabaseName("ix_referrals_referral_reference");

        // Supports the default list ordering (received date, newest first).
        builder.HasIndex(referral => referral.ReceivedDate)
            .HasDatabaseName("ix_referrals_received_date");

        // Supports filtering the list by status.
        builder.HasIndex(referral => referral.Status)
            .HasDatabaseName("ix_referrals_status");
    }
}
