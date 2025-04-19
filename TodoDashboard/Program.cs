using TodoDashboard.Components;
using TodoDashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ถ้ายังไม่มี Distributed Memory Cache ให้เพิ่ม
builder.Services.AddDistributedMemoryCache();

// ลงทะเบียน Session Service พร้อมกำหนดค่าต่าง ๆ ตามที่ต้องการ
builder.Services.AddSession(options =>
{
    // กำหนดเวลาที่ session จะหมดอายุ (idle timeout)
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    // options.Cookie.HttpOnly = true; // กำหนดค่าเพิ่มเติมหากต้องการ
});

builder.Services.AddHttpClient();
builder.Services.AddHttpClient("AuthorizedClient", client =>
{
    client.BaseAddress = new Uri("http://api:8080");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = true
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("AuthorizedClient"));
builder.Services.AddScoped<ApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSession();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
