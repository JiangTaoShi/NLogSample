### 背景

由于最近项目用到日志收集功能，目前现状如下

- 部署在 `windows server`、后期会容器化
- 需要持久化到`oracle`（支持分表按照天或者月）数据库及本地文件
- 要求对业务代码侵入少、简单易用、可配置
- 服务内部有互相(`http`)调用，可以追踪

作者对市面活跃的日志框架`serilog`和`nlog`觉得后者更优雅点（纯属个人言论），故提供此`demo` 来共同学习

1. ##### 添加相关引用

   ```c#
   	<ItemGroup>
   		<PackageReference Include="NLog.Database" Version="5.1.2" />
   		<PackageReference Include="NLog.Web.AspNetCore" Version="5.*" />
   		<PackageReference Include="NLog" Version="5.*" />
   		<PackageReference Include="Oracle.ManagedDataAccess.Core" Version="3.21.90" />
   
   		<PackageReference Include="Refit" Version="6.3.2" />
   		<PackageReference Include="Refit.HttpClientFactory" Version="6.3.2" />
   		<PackageReference Include="Refit.Newtonsoft.Json" Version="6.3.2" />
   	</ItemGroup>
   ```

2. ##### 配置`nlog`文件

   target 源分别输入到控制台、文件夹、数据库

   - 文件夹  
     1. `archiveAboveSize`  归档大小(以`bytes`算)，一旦超过这个值，文件就会被归档到你设定的文件夹里（`archiveFileName`）
   - 数据库
     1. `Log_TraceId` 链路追踪使用

   rules 规则 设置不同级别的日志输出到target

   ```C#
   <?xml version="1.0" encoding="utf-8" ?>
   <nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         autoReload="true"
         internalLogLevel="Info"
         internalLogFile="nlog-internal.log">
   
   
   	<variable name="logDir" value="${basedir}/log" />
   	<variable name="archiveDir" value="${var:logDir}/archive" />
   
   	<extensions>
   		<add assembly="NLog.Web.AspNetCore"/>
   	</extensions>
   
   	<targets  async="true">
   		<target name="console" xsi:type="ColoredConsole"
   				layout="${level:uppercase=true}|${logger:shortName=true}|${message}${onexception:${exception:format=tostring}}">
   		</target>
   
   		<target
   			 name="fileapplog"
   			 xsi:type="File"
   			 lineEnding="Default"
   			 autoFlush="true"
   			 concurrentWrites="true"
   			 archiveEvery="Day"
   			 fileName="${var:logDir}/applog.log"
   			 archiveFileName="${var:archiveDir}/applog_${date:format=yyyyMMdd}.log"
   			 archiveNumbering="sequence"
   			 archiveAboveSize="1024"
   			 maxArchiveFiles="720">
   			<layout xsi:type="CsvLayout">
   				<column name="time" layout="${longdate}"/>
   				<column name="level" layout="${level}"/>
   				<column name="logger" layout="${logger}"/>
   				<column name="message" layout="${message}"/>
   				<column name="exception" layout="${exception:tostring}}"/>
   			</layout>
   		</target>
   
   		<target name="oracleapplog" xsi:type="Database"
   				  dbProvider="Oracle.ManagedDataAccess.Client.OracleConnection, Oracle.ManagedDataAccess">
   			<connectionString>${var:logConnectionString}</connectionString>
   			<commandText>
   				insert into SYS_APPLOG (
   				LOG_ID,LOG_APP_NAME,LOG_TIMESTAMP,LOG_LEVEL, LOG_USER_NAME, LOG_MACHINE_NAME,
   				LOG_PROCESS_ID, LOG_THREAD_ID, LOG_PROCESS_NAME,LOG_THREAD_NAME,
   				LOG_MESSAGE,LOG_LOGGER,LOG_CALLSITE,LOG_EXCEPTION,LOG_URL,LOG_IP,LOG_USER_AGENT,LOG_TRACE_ID
   				)
   				values
   				(SEQ_LOGID.nextval,:Log_AppName,:Log_Timestamp, :Log_Level, :Log_UserName, :Log_MachineName,
   				:Log_ProcessId, :Log_ThreadId, :Log_ProcessName, :Log_ThreadName,
   				:Log_Message, :Log_Logger, :Log_Callsite, :Log_Exception, :Log_Url, :Log_IP, :Log_UserAgent,:Log_TraceId)
   			</commandText>
   			<parameter name=":Log_AppName" layout="NLogApp" />
   			<parameter name=":Log_Timestamp" layout="${longdate}" />
   			<parameter name=":Log_Level" layout="${level}" />
   			<parameter name=":Log_UserName" layout="${aspnet-User-Identity}" />
   			<parameter name=":Log_MachineName" layout="${machinename}" />
   			<parameter name=":Log_ProcessId" layout="${processid}" />
   			<parameter name=":Log_ThreadId" layout="${threadid}" />
   			<parameter name=":Log_ProcessName" layout="${processname}" />
   			<parameter name=":Log_ThreadName" layout="${threadname}" />
   			<parameter name=":Log_Message" layout="${message}" />       
   			<parameter name=":Log_Logger" layout="${logger}" />
   			<parameter name=":Log_CallSite" layout="${callsite:filename=true}" />
   			<parameter name=":Log_Exception" layout="${exception:tostring}" />
   			<parameter name=":Log_Url" layout="${aspnet-request-url:IncludePort=true:IncludeQueryString=true}" />
   			<parameter name=":Log_IP" layout="${aspnet-request-ip}" />
   			<parameter name=":Log_UserAgent" layout="${aspnet-request-useragent}" />
   			<parameter name=":Log_TraceId" layout="${aspnet-traceidentifier}" />
   		</target>
   	</targets>
   
   	<rules>
   		<logger name="System.*" minlevel="Warning" writeTo="console" />
   		<logger name="Microsoft.*" minlevel="Warning" writeTo="console" />
   		<logger name="NLogApp.*" minlevel="Info" writeTo="console" />
   
   		<logger name="System.*" minlevel="Info" writeTo="fileapplog" />
   		<logger name="Microsoft.*" minlevel="Info" writeTo="fileapplog" />
   		<logger name="NLogApp.*" minlevel="Info" writeTo="fileapplog" />
   
   		<logger name="System.*" minlevel="Warning" writeTo="oracleapplog" />
   		<logger name="Microsoft.*" minlevel="Warning" writeTo="oracleapplog" />
   		<logger name="NLogApp.*" minlevel="Info" writeTo="oracleapplog" />
   	</rules>
   </nlog>
   ```

   

3. ##### 后期构想

   目前由于项目资源受限，可以采用`EFK`中F Agent来负责采集日志，`webApp` 只负责将日志输出到控制台

   采用分布式链路组件排查问题及追踪，之前有用过`zipkin`、`skywalking`



### 参考项目

- [`NLog`](https://github.com/NLog/NLog/wiki/Getting-started-with-ASP.NET-Core-6)





