﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MjrChess.Trainer.Data;

namespace MjrChess.Trainer.Migrations
{
    [DbContext(typeof(PuzzleDbContext))]
    [Migration("20200131203321_AllowNullIncorrectMoves")]
    partial class AllowNullIncorrectMoves
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("MjrChess.Trainer.Models.Player", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset?>("CreatedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("LastModifiedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Site")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Players");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "Hustler",
                            Site = 2
                        },
                        new
                        {
                            Id = 2,
                            Name = "Noobie",
                            Site = 2
                        });
                });

            modelBuilder.Entity("MjrChess.Trainer.Models.PuzzleHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset?>("CreatedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("LastModifiedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("PuzzleId")
                        .HasColumnType("int");

                    b.Property<bool>("Solved")
                        .HasColumnType("bit");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("PuzzleId");

                    b.ToTable("PuzzleHistories");
                });

            modelBuilder.Entity("MjrChess.Trainer.Models.TacticsPuzzle", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("BlackPlayerId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("CreatedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("GameDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("IncorrectMovedFrom")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IncorrectMovedTo")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("IncorrectPieceMoved")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("LastModifiedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("MovedFrom")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MovedTo")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("PieceMoved")
                        .HasColumnType("int");

                    b.Property<string>("Position")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Site")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("WhitePlayerId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("BlackPlayerId");

                    b.HasIndex("WhitePlayerId");

                    b.ToTable("Puzzles");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            BlackPlayerId = 2,
                            CreatedDate = new DateTimeOffset(new DateTime(2020, 1, 31, 15, 33, 20, 398, DateTimeKind.Unspecified).AddTicks(1989), new TimeSpan(0, -5, 0, 0, 0)),
                            GameDate = new DateTimeOffset(new DateTime(2015, 2, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                            IncorrectMovedFrom = "d2",
                            IncorrectMovedTo = "d4",
                            IncorrectPieceMoved = 5,
                            LastModifiedDate = new DateTimeOffset(new DateTime(2020, 1, 31, 15, 33, 20, 402, DateTimeKind.Unspecified).AddTicks(4953), new TimeSpan(0, -5, 0, 0, 0)),
                            MovedFrom = "f3",
                            MovedTo = "f7",
                            PieceMoved = 1,
                            Position = "r1bqk1nr/pppp1ppp/2n5/2b1p3/2B1P3/5Q2/PPPP1PPP/RNB1K1NR w KQkq - 4 4",
                            WhitePlayerId = 1
                        });
                });

            modelBuilder.Entity("MjrChess.Trainer.Models.UserSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset?>("CreatedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("LastModifiedDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("UserSettings");
                });

            modelBuilder.Entity("MjrChess.Trainer.Models.UserSettingsXPlayer", b =>
                {
                    b.Property<int>("UserSettingsId")
                        .HasColumnType("int");

                    b.Property<int>("PlayerId")
                        .HasColumnType("int");

                    b.HasKey("UserSettingsId", "PlayerId");

                    b.HasIndex("PlayerId");

                    b.ToTable("UserSettingsXPlayers");
                });

            modelBuilder.Entity("MjrChess.Trainer.Models.PuzzleHistory", b =>
                {
                    b.HasOne("MjrChess.Trainer.Models.TacticsPuzzle", "Puzzle")
                        .WithMany("History")
                        .HasForeignKey("PuzzleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("MjrChess.Trainer.Models.TacticsPuzzle", b =>
                {
                    b.HasOne("MjrChess.Trainer.Models.Player", "BlackPlayer")
                        .WithMany()
                        .HasForeignKey("BlackPlayerId");

                    b.HasOne("MjrChess.Trainer.Models.Player", "WhitePlayer")
                        .WithMany()
                        .HasForeignKey("WhitePlayerId");
                });

            modelBuilder.Entity("MjrChess.Trainer.Models.UserSettingsXPlayer", b =>
                {
                    b.HasOne("MjrChess.Trainer.Models.Player", "Player")
                        .WithMany()
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MjrChess.Trainer.Models.UserSettings", "UserSettings")
                        .WithMany("PreferredPlayers")
                        .HasForeignKey("UserSettingsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
