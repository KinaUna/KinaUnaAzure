﻿// <auto-generated />
using System;
using KinaUnaMediaApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KinaUnaMediaApi.Migrations
{
    [DbContext(typeof(MediaDbContext))]
    [Migration("20181009082904_InitialMediaMigration")]
    partial class InitialMediaMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.3-rtm-32065")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("KinaUnaMediaApi.Models.Picture", b =>
                {
                    b.Property<int>("PictureId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AccessLevel");

                    b.Property<string>("Altitude");

                    b.Property<string>("Author");

                    b.Property<int>("CommentThreadNumber");

                    b.Property<string>("Latitude");

                    b.Property<string>("Location");

                    b.Property<string>("Longtitude");

                    b.Property<string>("Owners");

                    b.Property<int>("PictureHeight");

                    b.Property<string>("PictureLink")
                        .IsRequired()
                        .HasMaxLength(400);

                    b.Property<string>("PictureLink1200");

                    b.Property<string>("PictureLink600");

                    b.Property<int?>("PictureRotation");

                    b.Property<DateTime?>("PictureTime");

                    b.Property<int>("PictureWidth");

                    b.Property<int>("ProgenyId");

                    b.Property<string>("Tags");

                    b.HasKey("PictureId");

                    b.ToTable("PicturesDb");
                });

            modelBuilder.Entity("KinaUnaMediaApi.Models.Video", b =>
                {
                    b.Property<int>("VideoId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AccessLevel");

                    b.Property<string>("Author");

                    b.Property<int>("CommentThreadNumber");

                    b.Property<TimeSpan?>("Duration");

                    b.Property<string>("Owners");

                    b.Property<int>("ProgenyId");

                    b.Property<string>("Tags");

                    b.Property<string>("ThumbLink");

                    b.Property<string>("VideoLink");

                    b.Property<DateTime?>("VideoTime");

                    b.Property<int>("VideoType");

                    b.HasKey("VideoId");

                    b.ToTable("VideoDb");
                });
#pragma warning restore 612, 618
        }
    }
}
