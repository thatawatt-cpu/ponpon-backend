using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PonPon.Modules.Reviews.Infrastructure.Persistence;

#nullable disable

namespace PonPon.Modules.Reviews.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ReviewsDbContext))]
[Migration("20260709090000_InitialReviews")]
partial class InitialReviews
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasDefaultSchema("reviews")
            .HasAnnotation("ProductVersion", "10.0.4")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("PonPon.Modules.Reviews.Domain.Review", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<string>("Comment")
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<DateTime?>("DeletedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<DateTime?>("EditedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<Guid>("OrderId")
                .HasColumnType("uuid");

            b.Property<Guid>("OrderItemId")
                .HasColumnType("uuid");

            b.Property<Guid>("ProductId")
                .HasColumnType("uuid");

            b.Property<int>("Rating")
                .HasColumnType("integer");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            b.Property<DateTime?>("UpdatedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<Guid>("UserId")
                .HasColumnType("uuid");

            b.Property<Guid?>("VariantId")
                .HasColumnType("uuid");

            b.HasKey("Id");

            b.HasIndex("DeletedAtUtc");
            b.HasIndex("OrderId");
            b.HasIndex("OrderItemId")
                .IsUnique()
                .HasFilter("\"DeletedAtUtc\" IS NULL");
            b.HasIndex("ProductId");
            b.HasIndex("Status");
            b.HasIndex("UserId");
            b.HasIndex("VariantId");

            b.ToTable("reviews", "reviews");
        });

        modelBuilder.Entity("PonPon.Modules.Reviews.Domain.ReviewMedia", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<DateTime?>("DeletedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<int?>("DurationSec")
                .HasColumnType("integer");

            b.Property<long?>("FileSizeBytes")
                .HasColumnType("bigint");

            b.Property<string>("MimeType")
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            b.Property<Guid>("ReviewId")
                .HasColumnType("uuid");

            b.Property<int>("SortOrder")
                .HasColumnType("integer");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            b.Property<string>("ThumbnailUrl")
                .HasMaxLength(2048)
                .HasColumnType("character varying(2048)");

            b.Property<string>("Type")
                .IsRequired()
                .HasMaxLength(16)
                .HasColumnType("character varying(16)");

            b.Property<DateTime?>("UpdatedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Url")
                .IsRequired()
                .HasMaxLength(2048)
                .HasColumnType("character varying(2048)");

            b.HasKey("Id");

            b.HasIndex("DeletedAtUtc");
            b.HasIndex("ReviewId");
            b.HasIndex("Status");

            b.ToTable("review_media", "reviews");
        });

        modelBuilder.Entity("PonPon.Modules.Reviews.Domain.ReviewMedia", b =>
        {
            b.HasOne("PonPon.Modules.Reviews.Domain.Review", null)
                .WithMany("Media")
                .HasForeignKey("ReviewId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("PonPon.Modules.Reviews.Domain.Review", b =>
        {
            b.Navigation("Media");
        });
#pragma warning restore 612, 618
    }
}
