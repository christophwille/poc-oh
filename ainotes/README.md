# AI for Software Development

Note: Not really intended to be consumed in rendered mode!<br />
Note: Bluesky Likes / Saves / Reposts not necessarily included here.

If anything, watch this recording from Craft 2025, **especially** the last section on why China wins:

https://www.youtube.com/watch?v=w6WNitaeOPs (see also https://medium.com/mapai/why-open-source-ai-matters-a46a7d23ad0e)

Critical reporting is far and few between, so go read:

https://www.wheresyoured.at/

## Videos

* https://www.youtube.com/watch?v=K-Xv8D8NjTk&t=3138s How Anders uses AI + next sections (semantic search, vibe, futures)
* https://www.youtube.com/watch?v=RjfbvDXpFls Building pi in a World of Slop — Mario Zechner; see also https://newsletter.pragmaticengineer.com/p/building-pi-and-what-makes-self-modifying - Armin & Mario (watch video for more nuanced thinking of AI usage, what OSS really means to Mario)
* https://www.youtube.com/watch?v=xHHlhoRC8W4 Measuring the impact of AI (Laura Tacho)

## Guides

* https://danielmeppiel.github.io/agentic-sdlc-handbook/
* https://www.nibzard.com/agentic-handbook/
* https://github.com/github/awesome-copilot


## Generic

* https://www.normaltech.ai/p/why-ai-hasnt-replaced-software-engineers
* https://techcrunch.com/2026/05/27/tech-ceos-are-apparently-suffering-from-ai-psychosis/
* https://substack.jurgenappelo.com/p/stop-chasing-ai-tools explorer vs settler
* https://medium.com/feenk/rewilding-software-engineering-ca3ad1e612d8
* https://www.artificialintelligencemadesimple.com/i/192498139/case-study-2-how-github-copilot-lost-to-cursor-which-is-being-now-being-beaten-by-claude-code "The coding market shows the same pattern in faster motion. Copilot fit the old workflow and won the first phase. Cursor changed the value from autocomplete to higher-level delegation. Claude Code changed it again from delegation to autonomous execution. Each shift changed what developers were actually paying for, and each made the previous leader’s strengths less decisive."

* https://www.jamesshore.com/v2/blog/2026/you-need-ai-that-reduces-your-maintenance-costs
* https://medium.com/thrivve-partners/ai-wont-fix-your-broken-delivery-system-here-s-what-will-75ec7a1b0475 cycle time
* https://simonwillison.net/2026/Apr/13/bryan-cantrill/ "The problem is that LLMs inherently lack the virtue of laziness. Work costs nothing to an LLM. LLMs do not feel a need to optimize for their own (or anyone's) future time"
* German: https://www.heise.de/blog/Code-lesen-statt-Code-schreiben-Die-unterschaetzte-Senior-Disziplin-11288309.html
* German: https://www.golem.de/news/gartner-wie-ki-das-c-level-management-entwertet-2603-207029.html Richtig spannend fand ich die Abwehrhaltung auf die Frage. Vor allem weil sie gleich auf die Kosten ausweichen. Auch lustig wie sie Dinge plötzlich negieren, die beim Ersatz von Devs angeführt werden.

## Critical Thinking (unsorted)

* https://www.artificialintelligencemadesimple.com/p/anthropics-claude-mythos-launch-is misinformation and hype

https://www.mindprison.cc/p/verifier-loops-made-ai-coding-useful-vibeware-abandonware-technical-debt-consequences (No humble opinions here: one of the best articles)
* "Coding harnesses using a verifier-loop pattern are able to substantially increase the useful output of LLMs. This pattern is the only way to use LLMs to mitigate the hallucinations and lack of true reasoning."
* "I suppose it should not be too surprising that a technology built by scraping everything everyone ever created and claiming it as its own inspires the very same mindset in many of its most ardent proponents."
* "The abject hypocrisy of the AI labs complaining about IP theft is astounding: “Please don’t steal our stolen data.”"
* "Developers are starting to get fed up with bots trying to contribute to projects. It seems there are those who want to boost their GitHub activity, possibly for clout or as a reference for job applications, and are using AI to make the numbers go up."
* "As the cost of building applications approaches zero, their value becomes zero. A flood of applications that will never be maintained, as they are unsustainable. They will be created only to capture attention and then abandoned."

More

* https://www.blundergoat.com/articles/ai-makes-the-easy-part-easier-and-the-hard-part-harder "Writing code is the easy part of the job. It always has been. The hard part is investigation, understanding context, validating assumptions, and knowing why a particular approach is the right one for this situation."
* https://www.architecture-weekly.com/p/vibing-harness-and-ooda-loop "... the Internet is full of such people. They shout about what they did with Claude or how much progress LLM tools have made. Some even predict the end of coding. I already wrote that this is wrong perspective."
* https://brodzinski.com/2025/10/no-trust-autonomous-ai-agents.html transparency, alignment
* https://krasimirtsonev.com/blog/article/the-naked-truth-about-ai-assisted-coding

* https://mariozechner.at/posts/2026-03-25-thoughts-on-slowing-the-fuck-down/

* https://www.kellblog.com/the-brute-force-era-of-ai-and-what-comes-after/

* https://blog.tyler.fun/3mix3facggk24?auth_completed=true Claude Code is a Conflict of Interest

* https://kevlinhenney.medium.com/think-for-yourself-7d129aa959e3 Understand and improve on LLM-generated code

Criticism

* https://www.golem.de/news/meredith-whittaker-agentische-ki-als-sanfter-putsch-fuer-it-sicherheit-2512-203676.html One reason for the Avalonia Port
* https://garymarcus.substack.com/p/promises-are-cheap
* https://doctorow.medium.com/https-pluralistic-net-2025-08-18-seeing-like-a-billionaire-npcs-7a7746fe64e8

## Experience Reports (and related)

* https://testpappy.wordpress.com/2026/04/20/the-tech-radar-is-blinking-red/
* https://nrehiew.github.io/blog/minimal_editing/ ties in with "Why CoPilot code reviews always finds something new"
* https://devblogs.microsoft.com/dotnet/ten-months-with-cca-in-dotnet-runtime/ (=> need verifier loops)
* https://martinfowler.com/fragments/2026-01-08.html Top AI uses bei Antrophic:  Most usage is for debugging and helping understand existing code, Notable increase in using it for implementing new features.
* https://www.theregister.com/software/2025/07/11/ai-coding-tools-make-developers-slower-study-finds/1143832 the METR study
* https://martinfowler.com/articles/exploring-gen-ai/i-still-care-about-the-code.html
* https://newsletter.pragmaticengineer.com/p/two-years-of-using-ai

## Planning / Architecture / Decision Making

* https://www.hollandtech.net/claude-is-not-your-architect
* https://www.architecture-weekly.com/p/interactive-rubber-ducking-with-genai "GenAI tools are not great sparing partners. They’re Yes men. "
* https://youtu.be/qB7rsbDfmQg What AI augmentation means for tech leaders

## Cognitive Debt / Handing over too much control

* https://adamtornhill.substack.com/p/compressed-cognition-the-hidden-cost
* https://adamtornhill.substack.com/p/coding-is-dead-but-it-still-smells "Maintaining a constant focus is draining, and I find that I can usually only sustain that intense high pace for a couple of hours before needing a break. (Thankfully, I can let a long-running agent make progress while I sip coffee)." 
* https://simonwillison.net/2026/Feb/15/cognitive-debt/ AI cognitive debt vs tech debt
* https://medium.com/mapai/on-the-question-of-debt-aca1125d4a62 delegating comprehension is a risk with LLMs
* https://simonwillison.net/2026/Feb/15/the-ai-vampire/
* https://chrisloy.dev/post/2025/09/28/the-ai-coding-trap

Intentionally reducing AI usage

* https://www.linkedin.com/posts/asgaut-mjolne_after-four-months-of-claude-first-we-decided-ugcPost-7466023945259921408-Zkwi/ "runter vom Gas" 
* https://thoughts.hmmz.org/2026-05-31.html "On that last point, this technology is horrific for attention. It's a thermonuclear ADHD amplifier"
* https://adamtornhill.substack.com/p/how-much-of-my-writing-is-ai-generated why writing & effort is important
* https://simonwillison.net/2026/May/24/armin-ronacher/ prefer real short writing over long-format AI content

## Other OSS & AI

* https://simonwillison.net/2026/Jun/5/andreas-kling "A substantial patch used to imply substantial effort, and that effort was a reasonable proxy for good faith. That assumption no longer holds. [...]"
* https://simonwillison.net/2026/Apr/30/zig-anti-ai/ "Zig values contributors over their contributions."
* https://github.com/torvalds/linux/blob/master/Documentation/process/coding-assistants.rst roughly translates to "use as much AI as you want, in the way you want, but in the end you need to be fully responsible for any code that you used AI for" (via Pragmatic Engineer)
* https://petabridge.com/blog/ai-wont-kill-open-source/ AI is discovering and adopting open source libraries faster than humans ever could.
* https://simonwillison.net/2025/Dec/6/one-shot-decompilation/ that is just for "me" (ILSpy)

## Costs

Of course all Ed Zitron articles.

* https://newsletter.pragmaticengineer.com/i/201622200/2-new-trend-smart-model-routing
* https://www.llm-prices.com/
* https://newsletter.pragmaticengineer.com/p/the-pulse-token-spend-breaks-budgets token spending for various company sizes & alternatives
* https://newsletter.pragmaticengineer.com/p/the-pulse-tokenmaxxing-as-a-weird deficiences of measuring token usage
* https://ardalis.com/ai-benefits---but-at-what-cost/
* https://isaiprofitable.com/

## Fun

There has to be some fun in the whole thing ;-)

* https://www.youtube.com/watch?v=GgmaFPR17qY God is our Copilot