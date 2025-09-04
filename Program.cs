using System;
using System.Linq;
using CRMWebApp.Data;
using CRMWebApp.Models;
using CRMWebApp.Services;
using CRMWebApp.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
	.AddDefaultIdentity<ApplicationUser>(o =>
	{
		o.SignIn.RequireConfirmedAccount = true;
		o.User.RequireUniqueEmail = true;
	})
	.AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<AppDbContext>()
	.AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("AdminWithMfa",
		p => p.RequireRole("Admin").RequireClaim("amr", "mfa"));
});

builder.Services.AddScoped<IAuthorizationHandler, RecordOwnerAuthorizationHandler<Interaction>>();
builder.Services.AddScoped<IAuthorizationHandler, RecordOwnerAuthorizationHandler<Deal>>();
builder.Services.AddScoped<IAuthorizationHandler, RecordOwnerAuthorizationHandler<Client>>();

builder.Services.AddRazorPages(options =>
{
	options.Conventions.AuthorizeFolder("/Admin", "AdminWithMfa");
	options.Conventions.AllowAnonymousToPage("/Admin/Users/StopImpersonation");
	options.Conventions.AuthorizeFolder("/Dashboard");
});

builder.Services.AddTransient<IEmailSender, MailKitEmailSender>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IUserLifecycleService, UserLifecycleService>();

builder.Services.AddScoped<IDealScoringService, HeuristicDealScoringService>();
builder.Services.AddHostedService<DealProbabilityRecomputeHostedService>();

var app = builder.Build();

CommentFixer.Run(app.Services);

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;

	var db = services.GetRequiredService<AppDbContext>();
	await db.Database.MigrateAsync();

	var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
	var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();

	foreach (var r in new[] { "Admin", "User" })
		if (!await roleMgr.RoleExistsAsync(r))
			await roleMgr.CreateAsync(new IdentityRole(r));

	var adminEmail = "admin@crm.local";
	var admin = await userMgr.FindByEmailAsync(adminEmail);
	if (admin == null)
	{
		admin = new ApplicationUser
		{
			UserName = adminEmail,
			Email = adminEmail,
			EmailConfirmed = true,
			FullName = "Administrator"
		};
		await userMgr.CreateAsync(admin, "Admin!234"); await userMgr.AddToRoleAsync(admin, "Admin");
	}

	const string systemEmail = "system@crm.local";
	const string systemPassword = "ThisPasswordWillNeverBeUsed_#123";
	var system = await userMgr.FindByEmailAsync(systemEmail);
	if (system == null)
	{
		system = new ApplicationUser
		{
			UserName = systemEmail,
			Email = systemEmail,
			EmailConfirmed = true,
			FullName = "System",
			IsDeactivated = true,
		};
		var createRes = await userMgr.CreateAsync(system, systemPassword);
		if (!createRes.Succeeded)
		{
			throw new Exception("Failed to create System user: " +
				string.Join("; ", createRes.Errors.Select(e => $"{e.Code}:{e.Description}")));
		}
	}
	if (system.LockoutEnd != DateTimeOffset.MaxValue)
	{
		system.LockoutEnd = DateTimeOffset.MaxValue;
		await userMgr.UpdateSecurityStampAsync(system);
		await userMgr.UpdateAsync(system);
	}
}

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();