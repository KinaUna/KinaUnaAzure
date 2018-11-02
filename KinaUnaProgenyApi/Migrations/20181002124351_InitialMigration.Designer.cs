﻿// <auto-generated />
using System;
using KinaUnaProgenyApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KinaUnaProgenyApi.Migrations
{
    [DbContext(typeof(ProgenyDbContext))]
    [Migration("20181002124351_InitialMigration")]
    partial class InitialMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.3-rtm-32065")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("KinaUnaProgenyApi.Models.TimeLineItem", b =>
                {
                    b.Property<int>("TimeLineId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AccessLevel");

                    b.Property<string>("CreatedBy");

                    b.Property<DateTime>("CreatedTime");

                    b.Property<string>("ItemId");

                    b.Property<int>("ItemType");

                    b.Property<int>("ProgenyId");

                    b.Property<DateTime>("ProgenyTime");

                    b.HasKey("TimeLineId");

                    b.ToTable("TimeLineDb");
                });

            modelBuilder.Entity("KinaUnaProgenyApi.Models.UserAccess", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AccessLevel");

                    b.Property<int>("ProgenyId");

                    b.Property<string>("UserId");

                    b.HasKey("Id");

                    b.ToTable("UserAccessDb");
                });

            modelBuilder.Entity("KinaUnaWeb.Models.Progeny", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Admins");

                    b.Property<DateTime?>("BirthDay");

                    b.Property<string>("Name");

                    b.Property<string>("NickName");

                    b.Property<string>("PictureLink");

                    b.Property<string>("TimeZone");

                    b.HasKey("Id");

                    b.ToTable("ProgenyDb");
                });
#pragma warning restore 612, 618
        }
    }
}
