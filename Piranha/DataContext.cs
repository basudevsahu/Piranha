﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.Objects;
using System.Linq;
using System.Text;

namespace Piranha
{
	/// <summary>
	/// The Piranha DbContext
	/// </summary>
	public class DataContext : DbContext
	{
		#region Members
		/// <summary>
		/// Gets/sets the logged in identity in case this context is
		/// used outside of the HttpContext.
		/// </summary>
		internal static Guid Identity { get ; set ; }
		#endregion

		#region DbSets
		// External DbSets
		public DbSet<Entities.User> Users { get ; set ; }
		public DbSet<Entities.Group> Groups { get ; set ; }
		public DbSet<Entities.Permission> Permissions { get ; set ; }
		public DbSet<Entities.Param> Params { get ; set ; }
		public DbSet<Entities.Log> Logs { get ; set ; }
		public DbSet<Entities.PageTemplate> PageTemplates { get ; set ; }
		public DbSet<Entities.PostTemplate> PostTemplates { get ; set ; }
		public DbSet<Entities.RegionTemplate> RegionTemplates { get ; set ; }
		public DbSet<Entities.Namespace> Namespaces { get ; set ; }
		public DbSet<Entities.Permalink> Permalinks { get ; set ; }
		public DbSet<Entities.Category> Categories { get ; set ; }
		public DbSet<Entities.Media> Media { get ; set ; }
		public IQueryable<Entities.Property> Properties { get { return Set<Entities.Property>().Where(p => !p.IsDraft) ; } }
		public IQueryable<Entities.Region> Regions { get { return Set<Entities.Region>().Where(r => !r.IsDraft) ; } }
		public IQueryable<Entities.Post> Posts { get { return Set<Entities.Post>().Where(p => !p.IsDraft) ; } }
		public IQueryable<Entities.Page> Pages { get { return Set<Entities.Page>().Where(p => !p.IsDraft) ; } }
		public DbSet<Entities.Extension> Extensions { get ; set ; }
		public DbSet<Entities.Upload> Uploads { get ; set ; }
		public DbSet<Entities.Comment> Comments { get ; set ; }

		// Internal DbSets
		internal IQueryable<Entities.Page> PageDrafts { get { return Set<Entities.Page>().Where(p => p.IsDraft) ; } }
		internal IQueryable<Entities.Post> PostDrafts { get { return Set<Entities.Post>().Where(p => p.IsDraft) ; } }
		#endregion

		/// <summary>
		/// Default constructor. Creates a new db context.
		/// </summary>
		public DataContext() : base("Piranha") {
			((IObjectContextAdapter)this).ObjectContext.ObjectMaterialized += 
				new System.Data.Objects.ObjectMaterializedEventHandler(OnEntityLoad) ;
		}

		/// <summary>
		/// Logs in the given user when no HttpContext is available.
		/// </summary>
		/// <param name="login">Username</param>
		/// <param name="password">Password</param>
		/// <returns>If the login was successful</returns>
		public static bool Login(string login, string password) {
			var usr = Models.SysUser.Authenticate(login, password) ;

			if (usr != null) {
				Identity = usr.Id ;
				return true ;
			}
			return false ;
		}

		/// <summary>
		/// Logs in the default sys user.
		/// </summary>
		public static void LoginSys() {
			Identity = Piranha.Data.Database.SysUserId ;
		}

		/// <summary>
		/// Logs out the current user.
		/// </summary>
		public static void Logout() {
			Identity = Guid.Empty ;
		}

		/// <summary>
		/// Initializes the current model.
		/// </summary>
		/// <param name="modelBuilder">The model builder</param>
		protected override void OnModelCreating(DbModelBuilder modelBuilder) {
			modelBuilder.Configurations.Add(new Entities.Maps.UserMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.GroupMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.PermissionMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.ParamMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.LogMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.PageTemplateMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.PostTemplateMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.RegionTemplateMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.NamespaceMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.PermalinkMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.CategoryMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.MediaMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.PropertyMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.RegionMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.PostMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.PageMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.ExtensionMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.UploadMap()) ;
			modelBuilder.Configurations.Add(new Entities.Maps.CommentMap()) ;

			base.OnModelCreating(modelBuilder);
		}

		/// <summary>
		/// Event called when an entity has been loaded.
		/// </summary>
		/// <param name="sender">The sender</param>
		/// <param name="e">Event arguments</param>
		void OnEntityLoad(object sender, System.Data.Objects.ObjectMaterializedEventArgs e) {
			if (e.Entity is Entities.BaseEntity)
				((Entities.BaseEntity)e.Entity).OnLoad() ;
		}

		/// <summary>
		/// Saves the changes made to the context.
		/// </summary>
		/// <returns>The numbe of changes saved.</returns>
		public override int SaveChanges() {
			foreach (var entity in ChangeTracker.Entries()) {
				//
				// Call the correct software trigger.
				//
				if (entity.Entity is Entities.BaseEntity) {
					if (entity.State == EntityState.Added || entity.State == EntityState.Modified) {
						((Entities.BaseEntity)entity.Entity).OnSave(entity.State) ;
						//((Entities.BaseEntity)entity.Entity).OnInvalidate(entity.State) ;
					} else if (entity.State == EntityState.Deleted) {
						((Entities.BaseEntity)entity.Entity).OnDelete() ;
						//((Entities.BaseEntity)entity.Entity).OnInvalidate(entity.State) ;
					}
				}
			}
			return base.SaveChanges();
		}

		/// <summary>
		/// Validates an entity
		/// </summary>
		/// <param name="entity">The entity to validate</param>
		/// <param name="items">Optional params</param>
		/// <returns>The validation result</returns>
		protected override DbEntityValidationResult ValidateEntity(DbEntityEntry entity, IDictionary<object, object> items) {
			DbEntityValidationResult ret = null ;

			if (entity.Entity is Entities.BaseEntity)
				ret = ((Entities.BaseEntity)entity.Entity).OnValidate(entity) ;
			var valid = base.ValidateEntity(entity, items) ;
			if (ret != null) {
				foreach (var error in ret.ValidationErrors)
					valid.ValidationErrors.Add(error) ;
			}
			return valid ;
		}
	}
}