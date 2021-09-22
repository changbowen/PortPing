[![Build Status](https://changb0wen.visualstudio.com/PortPing/_apis/build/status/changbowen.PortPing?branchName=master)](https://changb0wen.visualstudio.com/PortPing/_build/latest?definitionId=5&branchName=master)

# PortPing

Windows command line tool for testing TCP port reachability with latency info and more. Requires .Net 4.5+.

Usage:
```
Usage: portping.exe host:port [-t timeout] [-s source[:port]]
    host:port           The hostname / IP address and port to connect to.
    -t timeout          Timeout in milliseconds to wait for each ping. Default is 5000ms.
    -s source[:port]    IP address of the source interface with optional port to use.
    -i interval         Interval in milliseconds to wait between each ping. Default is 1000ms.
```
