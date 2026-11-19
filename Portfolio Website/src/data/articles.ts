import type { Article } from './types';

/* ======================================================
   BLOG ARTICLES — shown on /blog/<slug>
   Key = the post's slug (title in lowercase with dashes),
   e.g. "5 UI mistakes that make users leave your app"
        -> "5-ui-mistakes-that-make-users-leave-your-app"

   sum:  one-line summary under the title
   body: blocks in order —
         "text"            a paragraph
         {h2:"Heading"}    section heading
         {h3:"Heading"}    small heading
         {quote:"text"}    highlighted quote
         {list:["a","b"]}  bullet list

   Posts without an entry show their summary and "coming soon".

   !! SAMPLE ARTICLES — written as placeholders. Replace them
   !! with your own writing before publishing.
   ====================================================== */
export const ARTICLES: Record<string, Article> = {
  "5-ui-mistakes-that-make-users-leave-your-app": {
    sum: "Small design problems that quietly push people away — and simple ways to fix them.",
    body: [
      "Most people don't delete an app because of one big problem. They leave because of many small moments that feel slow, confusing or annoying. After designing apps for fintech, fitness and food delivery clients, I keep seeing the same mistakes.",
      "Here are five of them, and what I do instead.",
      {h2: "1. Asking for too much, too early"},
      "Long sign-up forms before the user sees any value are the fastest way to lose them. Let people explore first, and ask for details only when they are needed.",
      {h2: "2. Buttons that don't look like buttons"},
      "Flat text links, low-contrast colours and tiny tap areas make users guess. Every main action should be obvious at first glance and easy to tap with one thumb.",
      {quote: "If a user has to think about where to tap, the design has already failed."},
      {h2: "3. Hidden costs and surprises"},
      "Showing fees or limits only at the last step breaks trust. Be honest early — a clear total on the first screen converts better than a surprise at checkout.",
      {h2: "4. No feedback after an action"},
      "When a user taps and nothing happens, they tap again, or leave. Every action needs a response: a loading state, a success message or a clear error.",
      {h3: "Simple feedback checklist"},
      {list: ["Show a loading state for anything over half a second", "Confirm success with a short message or animation", "Write errors in plain words and say how to fix them", "Never lose what the user already typed"]},
      {h2: "5. Too many things on one screen"},
      "Trying to show everything at once makes nothing stand out. Pick one main goal per screen and move the rest one tap away.",
      {h2: "The result"},
      "Fixing these five problems doesn't need a full redesign. Small, focused changes can make an app feel faster, clearer and more trustworthy — and that is what keeps people coming back."
    ]
  },
  "how-i-design-a-landing-page-that-converts": {
    sum: "My step-by-step process for landing pages that turn visitors into leads.",
    body: [
      "A landing page has one job: get the visitor to take one action. Everything on the page should help with that job, or it should go.",
      {h2: "Start with the message, not the layout"},
      "Before I open Figma, I write the headline, the main benefit and the call to action. If those three are not clear in words, no design will save the page.",
      {quote: "Visitors decide in a few seconds whether to stay. The headline has to do the work."},
      {h2: "Answer three questions at the top"},
      {list: ["What is this?", "Why should I care?", "What should I do next?"]},
      "If the first screen answers these three questions, most of the work is done.",
      {h2: "Build trust as they scroll"},
      "Below the hero I add proof: results, client logos, reviews and examples. Then I answer common doubts in a short FAQ, and repeat the call to action at the end.",
      {h2: "The result"},
      "A clear message, a simple structure and honest proof — that is what turns a nice-looking page into one that actually brings in leads."
    ]
  },
  "what-i-learned-from-my-first-50-fiverr-orders": {
    sum: "Lessons on clients, communication and pricing from my early freelance work.",
    body: [
      "My first Fiverr orders taught me more about running a design business than any course. Here is what I wish I had known on day one.",
      {h2: "Communication matters as much as design"},
      "Clients remember how the project felt. Quick replies, clear updates and honest timelines build trust even before they see the first design.",
      {h2: "Ask more questions before you start"},
      "Most revision rounds come from a weak brief. Now I always ask about goals, audience, examples they like and what success looks like.",
      {quote: "A good brief saves more time than a fast designer."},
      {h2: "What I do on every order now"},
      {list: ["Send a short summary of the brief before starting", "Share early wireframes, not just final designs", "Agree on the number of revisions up front", "Deliver organised, clearly named files"]},
      {h2: "The result"},
      "Happier clients, fewer revisions and more repeat orders. Good process turned one-time buyers into long-term clients."
    ]
  }
};
