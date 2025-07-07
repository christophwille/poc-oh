# dotnet test in net10

`dotnet test --report-trx --report-trx-filename testreport.trx`

Output:

```
Running tests from bin\Debug\net10.0\MTPEntrypoint.exe (net10.0|x64)
bin\Debug\net10.0\MTPEntrypoint.exe (net10.0|x64) passed (617ms)

  In process file artifacts produced:
    - bin\Debug\net10.0\TestResults\testreport.trx

Test run summary: Passed!
  total: 1
  failed: 0
  succeeded: 1
  skipped: 0
  duration: 2s 393ms
```

Of note: `Option '--report-trx-filename' has invalid arguments: file name argument must not contain path`

## Reference 

`dotnet test --help`

Output:

```
Description:
  .NET Test Command

Usage:
  dotnet test [options] [platform options] [extension options]

Options:
  --project <PROJECT_PATH>              Defines the path of the project file to run (folder name or full path). If not
                                        specified, it defaults to the current directory.
  --solution <SOLUTION_PATH>            Defines the path of the solution file to run. If not specified, it defaults to
                                        the current directory.
  --directory <DIRECTORY_PATH>          Defines the path of directory to run. If not specified, it defaults to the
                                        current directory.
  --test-modules <EXPRESSION>           Run tests for the specified test modules.
  --root-directory <ROOT_PATH>          The test modules have the specified root directory.
  --max-parallel-test-modules <NUMBER>  The max number of test modules that can run in parallel.
  -a, --arch <ARCH>                     The target architecture.
  -c, --configuration <CONFIGURATION>   The configuration to use for running tests. The default for most projects is
                                        'Debug'.
  -f, --framework <FRAMEWORK>           The target framework to run tests for. The target framework must also be
                                        specified in the project file.
  --os <OS>                             The target operating system.
  -r, --runtime <RUNTIME_IDENTIFIER>    The target runtime to test for.
  -v, --verbosity <LEVEL>               Set the MSBuild verbosity level. Allowed values are q[uiet], m[inimal],
                                        n[ormal], d[etailed], and diag[nostic].
  --no-restore                          Do not restore the project before building.
  --no-build                            Do not build the project before testing. Implies --no-restore.
  --no-ansi                             Disable ANSI output.
  --no-launch-profile                   Do not attempt to use launchSettings.json to configure the application.
  --no-launch-profile-arguments         Do not use arguments specified in launch profile to run the application.
  --no-progress                         Disable progress reporting.
  --output <Detailed|Normal>            Verbosity of test output.
  -?, -h, --help                        Show command line help.

Waiting for options and extensions...
Platform Options:
  --timeout                                 A global test execution timeout.
                                            Takes one argument as string in the format <value>[h|m|s] where 'value' is
                                            float.
  --minimum-expected-tests                  Specifies the minimum number of tests that are expected to run.
  --exit-on-process-exit                    Exit the test process if dependent process exits. PID must be provided.
  --info                                    Display .NET test application information.
  --diagnostic-verbosity                    Define the level of the verbosity for the --diagnostic.
                                            The available values are 'Trace', 'Debug', 'Information', 'Warning',
                                            'Error', and 'Critical'.
  --diagnostic-output-fileprefix            Prefix for the log file name that will replace '[log]_.'
  --config-file                             Specifies a testconfig.json file.
  --help                                    Show the command line help.
  --results-directory                       The directory where the test results are going to be placed.
                                            If the specified directory doesn't exist, it's created.
                                            The default is TestResults in the directory that contains the test
                                            application.
  --list-tests                              List available tests.
  --ignore-exit-code                        Do not report non successful exit value for specific exit codes
                                            (e.g. '--ignore-exit-code 8;9' ignore exit code 8 and 9 and will return 0
                                            in these case)
  --diagnostic-filelogger-synchronouswrite  Force the built-in file logger to write the log synchronously.
                                            Useful for scenario where you don't want to lose any log (i.e. in case of
                                            crash).
                                            Note that this is slowing down the test execution.
  --diagnostic-output-directory             Output directory of the diagnostic logging.
                                            If not specified the file will be generated inside the default
                                            'TestResults' directory.
  --diagnostic                              Enable the diagnostic logging. The default log level is 'Trace'.
                                            The file will be written in the output directory with the name
                                            log_[yyMMddHHmmssfff].diag

Extension Options:
  --no-progress          Disable reporting progress to screen.
  --settings             The path, relative or absolute, to the .runsettings file. For more information and examples on
                         how to configure test run, see
                         https://learn.microsoft.com/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-f
                         ile#the-runsettings-file
  --output               Output verbosity when reporting tests.
                         Valid values are 'Normal', 'Detailed'. Default is 'Normal'.
  --no-ansi              Disable outputting ANSI escape characters to screen.
  --report-trx           Enable generating TRX report
  --report-trx-filename  The name of the generated TRX report
  --test-parameter       Specify or override a key-value pair parameter. For more information and examples, see
                         https://learn.microsoft.com/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-f
                         ile#testrunparameters
  --filter               Filters tests using the given expression. For more information, see the Filter option details
                         section. For more information and examples on how to use selective unit test filtering, see
                         https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests.
```

