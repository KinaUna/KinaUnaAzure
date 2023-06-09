﻿// <auto-generated />
using System;
using KinaUna.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KinaUna.IDP.Migrations.MediaDb
{
    [DbContext(typeof(MediaDbContext))]
    [Migration("20190313094311_InitialMediaDbMigration")]
    partial class InitialMediaDbMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.2-servicing-10034")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("KinaUna.Data.Models.Comment", b =>
                {
                    b.Property<int>("CommentId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Author");

                    b.Property<string>("CommentText");

                    b.Property<int>("CommentThreadNumber");

                    b.Property<DateTime>("Created");

                    b.Property<string>("DisplayName");

                    b.HasKey("CommentId");

                    b.ToTable("CommentsDb");
                });

            modelBuilder.Entity("KinaUna.Data.Models.CommentThread", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("CommentThreadId");

                    b.Property<int>("CommentsCount");

                    b.HasKey("Id");

                    b.ToTable("CommentThreadsDb");
                });

            modelBuilder.Entity("KinaUna.Data.Models.Picture", b =>
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

            modelBuilder.Entity("KinaUna.Data.Models.Video", b =>
                {
                    b.Property<int>("VideoId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AccessLevel");

                    b.Property<string>("Altitude");

                    b.Property<string>("Author");

                    b.Property<int>("CommentThreadNumber");

                    b.Property<TimeSpan?>("Duration");

                    b.Property<string>("Latitude");

                    b.Property<string>("Location");

                    b.Property<string>("Longtitude");

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
