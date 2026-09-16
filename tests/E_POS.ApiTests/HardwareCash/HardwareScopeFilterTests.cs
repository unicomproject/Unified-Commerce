using System.Reflection;
using System.Security.Claims;
using E_POS.Api.Common;
using E_POS.Api.Controllers.V1.Tenant.HardwareCash;
using E_POS.Api.Controllers.V1.Tenant.OutletTillDevice;
using E_POS.Application.Modules.Tenant.HardwareCash.Contracts;
using E_POS.Application.Modules.Tenant.HardwareCash.Dtos.TenantAdmin;
using E_POS.Domain.Modules.Tenant.HardwareCash.Entities;
using E_POS.Domain.Modules.Tenant.OutletTillDevice.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace E_POS.ApiTests.HardwareCash;
public class HardwareScopeFilterTests
{
    static readonly Guid Tenant=Guid.NewGuid(), User=Guid.NewGuid(), Outlet=Guid.NewGuid(), TillId=Guid.NewGuid(), Device=Guid.NewGuid();
    [Theory]
    [InlineData("GetConfigurations", true, true)]
    [InlineData("SaveConfiguration", true, true)]
    [InlineData("CreateTest", true, true)]
    [InlineData("CompleteTest", true, true)]
    [InlineData("GetHistory", true, true)]
    [InlineData("GetRemoteTests", true, true)]
    [InlineData("GetRemoteTests", false, true)]
    [InlineData("GetRemoteTests", true, false)]
    [InlineData("CreateTest", false, true)]
    [InlineData("CreateTest", true, false)]
    public async Task NativeRuntimeRequiresVerifiedProofAndExactTill(string action, bool proof, bool mapped) =>
        await Check(action, new(), proof && mapped, otherAssignment: !mapped, native: true, proof: proof);
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task TelemetryRequiresMappedHardware(bool other, bool expected) =>
        await Check("ReportTest", new(){["request"]=new PosHardwareTestResultRequest(Device,"SCAN","PASSED",PosDeviceId:Device)}, expected, otherAssignment:other,native:true,proof:true);
    [Fact]
    public void DeviceProofRunsBeforeScopeAndTransaction() {
        foreach(var type in new[]{typeof(E_POS.Api.Controllers.PosHardwareController),typeof(PosHardwareTelemetryController)}) {
            var filters=type.GetCustomAttributes<TypeFilterAttribute>().ToArray();
            Assert.True(filters.Single(f=>f.ImplementationType==typeof(PosDeviceProofFilter)).Order < filters.Single(f=>f.ImplementationType==typeof(HardwareScopeFilter)).Order);
            Assert.True(filters.Single(f=>f.ImplementationType==typeof(HardwareScopeFilter)).Order < filters.Single(f=>f.ImplementationType==typeof(HardwareTelemetryTransactionFilter)).Order);
        }
    }
    [Theory]
    [InlineData("AssignToTill", true, true)]
    [InlineData("AssignToTill", false, false)]
    [InlineData("GetHardwareReadiness", true, true)]
    [InlineData("GetHardwareReadiness", false, false)]
    [InlineData("List", true, true)]
    [InlineData("Dashboard", true, true)]
    [InlineData("AssignToPosDevice", true, false)]
    [InlineData("CreateOptions", true, true)]
    public async Task RestrictsEveryTargetBeforeInvokingAction(string action, bool selectedTill, bool expected)
    {
        var args=new Dictionary<string,object?> { ["tillId"]=selectedTill?TillId:Guid.NewGuid(), ["id"]=selectedTill?TillId:Guid.NewGuid(), ["request"]=new TenantAdminHardwareAssignmentRequest(Device) };
        await Check(action,args,expected);
    }
    [Theory]
    [InlineData("GetById")]
    [InlineData("TestHistory")]
    [InlineData("Activity")]
    [InlineData("Update")]
    public async Task DeviceAssignedToOtherTillIsDenied(string action) =>
        await Check(action,new(){["id"]=Device},false,otherAssignment:true);
    [Fact]
    public async Task ForeignOutletCreateIsDenied() => await Check("Create",new(){["request"]=new TenantAdminHardwareDeviceCreateRequest(Guid.NewGuid(),"P","Printer","RECEIPT_PRINTER","USB")},false);
    [Fact]
    public async Task OwnOutletCreateIsAllowed() => await Check("Create",new(){["request"]=new TenantAdminHardwareDeviceCreateRequest(Outlet,"P","Printer","RECEIPT_PRINTER","USB")},true);
    [Fact]
    public async Task ParentOnOtherTillIsDenied() => await Check("Create",new(){["request"]=new TenantAdminHardwareDeviceCreateRequest(Outlet,"D","Drawer","CASH_DRAWER","USB",ConfigJson:"{\"parentPrinterId\":\""+Device+"\"}")},false,otherAssignment:true);
    [Fact]
    public async Task MalformedRuleFailsClosed() => await Check("CreateOptions",new(),false,invalid:true);
    [Fact]
    public async Task ReleaseOtherTillIsDenied() => await Check("Release",new(){["assignmentId"]=Guid.NewGuid()},false,otherAssignment:true);

    [Theory]
    [InlineData("List")]
    [InlineData("Dashboard")]
    public async Task ForeignOutletQueryIsDenied(string action) => await Check(action,new(){["outletId"]=Guid.NewGuid()},false);
    [Theory]
    [InlineData("StartTestAll", true)]
    [InlineData("StartTestAll", false)]
    [InlineData("GetTestAll", true)]
    [InlineData("GetTestAll", false)]
    public async Task RemoteBatchRequiresExactTill(string action, bool allowed) =>
        await Check(action,new(){["tillId"]=allowed?TillId:Guid.NewGuid()},allowed);
    static async Task Check(string name, Dictionary<string,object?> args,bool expected,bool otherAssignment=false,bool invalid=false, bool native=false, bool proof=false)
    {
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["HardwareAccess:Restrictions:0:TenantId"]=Tenant.ToString(),["HardwareAccess:Restrictions:0:UserId"]=User.ToString(),
            ["HardwareAccess:Restrictions:0:OutletId"]=Outlet.ToString(),["HardwareAccess:Restrictions:0:TillId"]=(invalid?Guid.Empty:TillId).ToString(),
        }).Build();
        var repo=DispatchProxy.Create<ITenantAdminHardwareRepository,Repo>();((Repo)(object)repo).Other=otherAssignment;
        var http=new DefaultHttpContext { User=new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub",User.ToString()),new Claim("tenant_id",Tenant.ToString())],"test")) };
        if(proof) http.Items["VerifiedPosDeviceId"]=Device;
        var type=name=="ReportTest"?typeof(PosHardwareTelemetryController):native?typeof(E_POS.Api.Controllers.PosHardwareController):name=="GetHardwareReadiness"?typeof(TenantAdminTillsController):typeof(TenantAdminHardwareDevicesController);
        var descriptor=new ControllerActionDescriptor { MethodInfo=type.GetMethod(name)!, ControllerTypeInfo=type.GetTypeInfo() };
        object controller=name=="ReportTest"?new PosHardwareTelemetryController(null!,new TenantRequestContextFactory()):native?new E_POS.Api.Controllers.PosHardwareController(null!,new TenantRequestContextFactory()):new object();
        var ac=new ActionContext(http,new RouteData(),descriptor);var context=new ActionExecutingContext(ac,[],args,controller);bool invoked=false;
        var queryScope = new HardwareQueryScope();
        await new HardwareScopeFilter(new TenantRequestContextFactory(),config,repo,queryScope).OnActionExecutionAsync(context,()=>{invoked=true;return Task.FromResult(new ActionExecutedContext(ac,[],new object()));});
        if(expected && (name=="List" || name=="Dashboard")) { Assert.Equal(Tenant,queryScope.TenantId); Assert.Equal(User,queryScope.UserId); Assert.Equal(Outlet,queryScope.OutletId); Assert.Equal(TillId,queryScope.TillId); }
        Assert.Equal(expected,invoked);if(!expected)Assert.Equal(403,Assert.IsType<ObjectResult>(context.Result).StatusCode);
    }
    public class Repo : DispatchProxy {
        public bool Other;
        protected override object? Invoke(MethodInfo? method,object?[]? args) {
            Assert.Equal(Tenant,(Guid)args![0]!);
            var assignment=HardwareDeviceAssignment.Create(Guid.NewGuid(),Tenant,Outlet,Device,Other?Guid.NewGuid():TillId,null,false,User,DateTimeOffset.UtcNow);
            return method!.Name switch {
                "IsTrustedPosAssignedToTillAsync"=>CheckMapping(args),
                "GetTillAsync"=>Task.FromResult<Till?>(Till.Create(TillId,Tenant,Outlet,"Front Till 01","Front",1,"FRONT-01","FIXED",0,"LKR",true,"ACTIVE",User,DateTimeOffset.UtcNow)),
                "GetDetailAsync"=>Task.FromResult<HardwareDeviceDetailRow?>(new(HardwareDevice.Create(Device,Tenant,Outlet,null,"P","Printer","RECEIPT_PRINTER","USB",null,null,null,null,null,null,"ACTIVE",User,DateTimeOffset.UtcNow),"Store",assignment)),
                "GetAssignmentAsync"=>Task.FromResult<HardwareDeviceAssignment?>(assignment),
                _=>throw new InvalidOperationException("Unexpected repository call: "+method.Name)
            };
        }
        private Task<bool> CheckMapping(object?[] args) {
            Assert.Equal(Outlet, (Guid)args[1]!);
            Assert.Equal(TillId, (Guid)args[2]!);
            Assert.Equal(Device, (Guid)args[3]!);
            return Task.FromResult(!Other);
        }
    }
}


