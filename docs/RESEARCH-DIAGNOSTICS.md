# Research Machine input diagnostics

The reported Automate failure has not been reproduced in a running game. Source review confirms that current Automate uses native `AttemptAutoLoad`, which reaches the same `PlaceInMachine` and machine-output rules as manual insertion. No separate Automate patch has been added.

On a build containing this diagnostic:

1. Select the affected Researcher seed in the toolbar and face the Research Machine.
2. Run `cropbreeding_research_check` in the SMAPI console.
3. Include the resulting SMAPI log when reporting the problem. Also report whether manual insertion works and whether Automate's overlay places the machine and chest in the same active group.

The command reports mod versions, selected seed ID/traits/quantity, trait cap and eligibility, machine contents/timer, the `machine_input` context tag, loaded native machine rule matching and output callback resolution. It does not consume inputs, call output generation, roll traits, or change the stored result. It runs only when explicitly invoked.

Without the diagnostic build, a SMAPI log after attempting manual insertion is still useful. A valid native probe is not proof that output creation or a complete Automate cycle succeeds. Earlier callback tests used doubles and did not test an installed Automate group.
