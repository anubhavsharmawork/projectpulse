/**
 * Help content data model and default content for Project Pulse.
 *
 * Design goals
 * ────────────
 * • Culturally neutral language — no region-specific idioms or metaphors.
 * • Written for ages 13+ with zero assumed technical knowledge.
 * • Structured for translation — every user-visible string lives here.
 * • Extensible — add categories/articles without touching component code.
 *
 * To add a new language, duplicate the `en` content object, translate every
 * string value, and register the locale key in HelpPanelComponent.
 */

/* ── Interfaces ── */

export interface HelpStep {
  /** Sequential label shown to the left of the step (e.g. "1", "2"). */
  order: number;
  /** Plain-language instruction. */
  text: string;
}

export interface HelpArticle {
  /** Stable identifier — never rename once published. */
  id: string;
  /** Short, scannable title displayed in search results and headings. */
  title: string;
  /** One-sentence summary shown beneath the title. */
  summary: string;
  /** Full explanation displayed when the article is expanded. */
  body: string;
  /** Optional ordered walkthrough steps for task-oriented guides. */
  steps?: HelpStep[];
  /** Searchable keywords that do not appear in the title or body. */
  keywords: string[];
}

export interface HelpCategory {
  /** Stable identifier. */
  id: string;
  /** Category heading shown in the sidebar / accordion. */
  title: string;
  /** SVG icon path data (24×24 viewBox) — culturally neutral symbol. */
  icon: string;
  /** Articles within this category, ordered for progressive disclosure. */
  articles: HelpArticle[];
}

export interface HelpContent {
  /** BCP-47 locale tag (e.g. "en", "es", "ja"). */
  locale: string;
  /** Heading displayed at the top of the help panel. */
  panelTitle: string;
  /** Placeholder text inside the search field. */
  searchPlaceholder: string;
  /** Shown when no search results match. */
  noResultsMessage: string;
  /** Label for the "Getting Started" quick-link. */
  gettingStartedLabel: string;
  /** Label for the "Back" navigation control. */
  backLabel: string;
  /** Ordered list of help categories. */
  categories: HelpCategory[];
}

/* ── Default English content ── */

export const HELP_CONTENT_EN: HelpContent = {
  locale: 'en',
  panelTitle: 'Help Center',
  searchPlaceholder: 'Search for help\u2026',
  noResultsMessage: 'No matching topics found. Try different words or browse the categories below.',
  gettingStartedLabel: 'Getting Started',
  backLabel: 'Back',
  categories: [
    /* ─── Getting Started ─── */
    {
      id: 'getting-started',
      title: 'Getting Started',
      icon: 'M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5',
      articles: [
        {
          id: 'gs-overview',
          title: 'What is Project Pulse?',
          summary: 'A quick overview of the application and what you can do with it.',
          body: 'Project Pulse is an accessible project management application that helps you organise work, track progress, and collaborate with your team. You can create projects, break work into smaller pieces, visualise progress on a board, and keep everyone informed with real-time notifications.',
          keywords: ['overview', 'introduction', 'about', 'what is', 'purpose']
        },
        {
          id: 'gs-create-account',
          title: 'Creating your account',
          summary: 'How to sign up and get started in a few moments.',
          body: 'You only need an email address and a password to create an account. After signing up you can start creating projects straight away.',
          steps: [
            { order: 1, text: 'Select "Register" in the top-right corner of the page.' },
            { order: 2, text: 'Enter your email address and choose a password.' },
            { order: 3, text: 'Accept the terms and select "Register".' },
            { order: 4, text: 'You will be taken to the Projects page, ready to begin.' }
          ],
          keywords: ['register', 'sign up', 'new account', 'join']
        },
        {
          id: 'gs-login',
          title: 'Logging in',
          summary: 'How to access your account, including the demo option.',
          body: 'Use your email and password to log in. If you want to explore the application first, select "Try Demo Login" on the home page — this uses a sample account so you can look around without creating anything.',
          steps: [
            { order: 1, text: 'Select "Login" in the top-right corner.' },
            { order: 2, text: 'Enter your email address and password.' },
            { order: 3, text: 'Select "Login" to continue.' }
          ],
          keywords: ['sign in', 'log in', 'demo', 'access', 'credentials']
        },
        {
          id: 'gs-navigation',
          title: 'Finding your way around',
          summary: 'How the navigation bar, pages, and keyboard shortcuts work.',
          body: 'The bar at the top of every page contains links to the main areas: Projects, Dashboard, Time Tracking, Audit Logs, and Admin. When you are logged in you will also see a notification bell and your organisation name. You can use the Tab key to move between controls and Enter or Space to activate them.',
          keywords: ['navigate', 'menu', 'toolbar', 'header', 'links', 'keyboard']
        },
        {
          id: 'gs-timezone',
          title: 'Timezone settings',
          summary: 'How your timezone is detected and how to change it.',
          body: 'When you log in, Project Pulse automatically detects your timezone from your device and saves it with your account. All dates and times throughout the application are then displayed in your local timezone. If the detected timezone is not correct, or if you travel and want to keep a fixed timezone, you can change it at any time from the user preferences menu in the top navigation bar.',
          steps: [
            { order: 1, text: 'Log in to your account — your timezone is detected automatically.' },
            { order: 2, text: 'Select your username or the preferences icon near the top-right corner of the page.' },
            { order: 3, text: 'Choose your preferred timezone from the list.' },
            { order: 4, text: 'Select "Save". All dates and times will update to reflect your choice.' }
          ],
          keywords: ['timezone', 'time zone', 'clock', 'UTC', 'offset', 'detect', 'auto', 'override', 'local time', 'preferences']
        }
      ]
    },

    /* ─── Projects ─── */
    {
      id: 'projects',
      title: 'Projects',
      icon: 'M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM14 14h7v7h-7z',
      articles: [
        {
          id: 'proj-create',
          title: 'Creating a project',
          summary: 'Set up a new project with a name, description, and domain.',
          body: 'A project is the top-level container for all your work. Give it a clear name so everyone on the team knows what it covers.',
          steps: [
            { order: 1, text: 'Go to the Projects page.' },
            { order: 2, text: 'Fill in the "Project Name" field. A description is optional but helpful.' },
            { order: 3, text: 'Choose a domain if your organisation uses them (for example IT or Healthcare).' },
            { order: 4, text: 'Select "Create" to save the project.' }
          ],
          keywords: ['new project', 'add project', 'domain', 'category']
        },
        {
          id: 'proj-filter',
          title: 'Filtering projects',
          summary: 'Switch between All, My Projects, and Public views.',
          body: 'The tabs above the project list let you filter what you see. "All Projects" shows every project you have access to. "My Projects" shows only the ones you created or are a member of. "Public Projects" shows projects visible to everyone in the organisation.',
          keywords: ['filter', 'tabs', 'all', 'mine', 'public', 'visibility']
        },
        {
          id: 'proj-team',
          title: 'Managing project members',
          summary: 'Add or remove team members and set their roles.',
          body: 'Open a project and go to the Team page to manage who has access. You can assign roles so that each person has the right level of permission.',
          steps: [
            { order: 1, text: 'Open the project you want to manage.' },
            { order: 2, text: 'Select the "Team" tab.' },
            { order: 3, text: 'Enter the username of the person you want to add.' },
            { order: 4, text: 'Choose a role and select "Assign".' }
          ],
          keywords: ['members', 'team', 'add user', 'role', 'assign', 'permission']
        }
      ]
    },

    /* ─── Work Items ─── */
    {
      id: 'work-items',
      title: 'Work Items',
      icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2',
      articles: [
        {
          id: 'wi-hierarchy',
          title: 'Understanding the work hierarchy',
          summary: 'How the three levels of work items relate to each other.',
          body: 'Work is organised in three levels. Level 1 is the largest goal, Level 2 breaks it into smaller pieces, and Level 3 contains the individual actionable items. The exact names for each level change depending on your project\u2019s domain. For example, an IT project uses Epic \u2192 User Story \u2192 Task, a Healthcare project uses Initiative \u2192 Action Item \u2192 Task, and a Construction project uses Phase \u2192 Activity \u2192 Punch Item. The labels are set automatically when you choose a domain for your project.',
          keywords: ['epic', 'story', 'task', 'hierarchy', 'levels', 'structure', 'parent', 'child', 'initiative', 'phase', 'activity', 'operation', 'program', 'feature', 'action item', 'work package', 'punch item']
        },
        {
          id: 'wi-domains',
          title: 'Domain-specific terminology',
          summary: 'Each domain uses its own names for the three work item levels.',
          body: 'When you create a project, you can choose a domain such as IT, Healthcare, Construction, Public Safety, Infrastructure, Economic Development, or Technology. Each domain uses terminology that fits its industry. The application automatically relabels work items to match. If no domain is chosen, the default labels are Epic, User Story, and Task.',
          steps: [
            { order: 1, text: 'IT: Epic \u2192 User Story \u2192 Task' },
            { order: 2, text: 'Healthcare: Initiative \u2192 Action Item \u2192 Task' },
            { order: 3, text: 'Construction: Phase \u2192 Activity \u2192 Punch Item' },
            { order: 4, text: 'Public Safety: Operation \u2192 Action Plan \u2192 Task' },
            { order: 5, text: 'Infrastructure: Program \u2192 Work Package \u2192 Task' },
            { order: 6, text: 'Economic Development: Program \u2192 Initiative \u2192 Task' },
            { order: 7, text: 'Technology: Epic \u2192 Feature \u2192 Task' }
          ],
          keywords: ['domain', 'IT', 'healthcare', 'construction', 'public safety', 'infrastructure', 'economic development', 'technology', 'labels', 'terminology', 'names', 'epic', 'initiative', 'phase', 'operation', 'program', 'feature', 'activity', 'action plan', 'work package', 'punch item']
        },
        {
          id: 'wi-create',
          title: 'Creating work items',
          summary: 'How to add work items at each level of the hierarchy.',
          body: 'From the Work Items page, use the form at the top of each section to create items. Give each one a clear title so the purpose is obvious at a glance. The labels you see depend on the domain your project uses.',
          steps: [
            { order: 1, text: 'Open your project and go to Work Items.' },
            { order: 2, text: 'In the top-level section, enter a title and select "Add".' },
            { order: 3, text: 'Expand the top-level item to add second-level items inside it.' },
            { order: 4, text: 'Expand a second-level item to add individual tasks inside it.' }
          ],
          keywords: ['add', 'create', 'new', 'work item', 'epic', 'story', 'task', 'initiative', 'phase', 'activity']
        },
        {
          id: 'wi-workflow',
          title: 'Moving items through workflow states',
          summary: 'Transition work items from one state to the next.',
          body: 'Each work item has a workflow state. The state names depend on your project\u2019s domain — for example, an IT project might use "Backlog \u2192 In Progress \u2192 Done", while a Construction project might use "Planning \u2192 In Construction \u2192 Completed". When the status of a piece of work changes, select the transition button to move it to the next state. The available transitions depend on how the workflow has been configured for your project.',
          keywords: ['workflow', 'state', 'transition', 'status', 'move', 'progress', 'done', 'backlog', 'planning', 'completed']
        }
      ]
    },

    /* ─── Workflow Board ─── */
    {
      id: 'workflow-board',
      title: 'Workflow Board',
      icon: 'M4 4h5v16H4zM10 4h5v16h-5zM16 4h5v16h-5z',
      articles: [
        {
          id: 'board-overview',
          title: 'Using the Workflow Board',
          summary: 'A visual way to see all work items organised by their current state.',
          body: 'The board displays columns for each workflow state. Cards appear in the column that matches their current state. You can move cards between columns by selecting the transition button on each card.',
          keywords: ['board', 'kanban', 'columns', 'visual', 'cards', 'drag']
        },
        {
          id: 'board-config',
          title: 'Configuring workflow states',
          summary: 'Add, rename, or reorder the columns on your board.',
          body: 'Go to the Workflow Config page for your project to change the available states, set which state is the starting point, and define allowed transitions between states.',
          steps: [
            { order: 1, text: 'Open your project and go to Workflow Config.' },
            { order: 2, text: 'Add new states or rename existing ones.' },
            { order: 3, text: 'Set the initial and final states.' },
            { order: 4, text: 'Define which transitions are allowed between states.' }
          ],
          keywords: ['configure', 'config', 'states', 'transitions', 'columns', 'setup']
        }
      ]
    },

    /* ─── Dashboard ─── */
    {
      id: 'dashboard',
      title: 'Dashboard',
      icon: 'M4 5a1 1 0 011-1h4a1 1 0 011 1v5a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM14 5a1 1 0 011-1h4a1 1 0 011 1v5a1 1 0 01-1 1h-4a1 1 0 01-1-1V5zM4 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1v-4zM14 13a1 1 0 011-1h4a1 1 0 011 1v6a1 1 0 01-1 1h-4a1 1 0 01-1-1v-6z',
      articles: [
        {
          id: 'dash-kpis',
          title: 'Understanding the Dashboard',
          summary: 'What the numbers and charts on the Dashboard mean.',
          body: 'The Dashboard shows key numbers at a glance: how many tasks are complete, how many are overdue, and how the team is performing. Use the domain filter at the top to view metrics for a specific area of work.',
          keywords: ['metrics', 'kpi', 'completion', 'overdue', 'utilization', 'chart', 'statistics']
        },
        {
          id: 'dash-domains',
          title: 'Filtering by domain',
          summary: 'Focus the Dashboard on a specific area of work.',
          body: 'If your organisation uses domains (such as IT, Healthcare, or Construction), you can select one from the dropdown at the top of the Dashboard. This filters every metric on the page so you see only the data for that area.',
          keywords: ['domain', 'filter', 'IT', 'healthcare', 'construction']
        }
      ]
    },

    /* ─── Time Tracking ─── */
    {
      id: 'time-tracking',
      title: 'Time Tracking',
      icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
      articles: [
        {
          id: 'time-log',
          title: 'Logging time',
          summary: 'Record how long you spent on a piece of work.',
          body: 'Time entries help the team understand where effort is being spent. Each entry is linked to a project and optionally to a specific work item.',
          steps: [
            { order: 1, text: 'Go to the Time Tracking page.' },
            { order: 2, text: 'Select the project you worked on.' },
            { order: 3, text: 'Optionally search for and select a specific work item.' },
            { order: 4, text: 'Enter the number of hours, a date, and a short note.' },
            { order: 5, text: 'Select "Log Time" to save.' }
          ],
          keywords: ['time', 'hours', 'log', 'track', 'effort', 'entry']
        }
      ]
    },

    /* ─── Notifications ─── */
    {
      id: 'notifications',
      title: 'Notifications',
      icon: 'M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0',
      articles: [
        {
          id: 'notif-overview',
          title: 'How notifications work',
          summary: 'Stay informed about changes that affect you.',
          body: 'The bell icon in the top bar shows your unread notification count. Notifications appear when work items change state, when someone assigns work to you, when a task becomes overdue, or when you are mentioned in a comment. Select the bell to open the list and select an item to view its details.',
          keywords: ['bell', 'unread', 'alert', 'mention', 'real-time', 'update']
        },
        {
          id: 'notif-mentions',
          title: 'Mentions',
          summary: 'Get notified when someone mentions you in a comment.',
          body: 'When a team member types your username in a comment, you will receive a notification. This makes it easy to draw someone\'s attention to a discussion without leaving the project.',
          keywords: ['mention', '@', 'comment', 'tag', 'reference']
        }
      ]
    },

    /* ─── Administration ─── */
    {
      id: 'admin',
      title: 'Administration',
      icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.066 2.573c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.573 1.066c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.066-2.573c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z M15 12a3 3 0 11-6 0 3 3 0 016 0z',
      articles: [
        {
          id: 'admin-overview',
          title: 'Admin panel overview',
          summary: 'Manage users and application settings.',
          body: 'The Admin page lets you manage users, roles, and application-wide settings. Access it from the "Admin" link in the navigation bar. Some actions may require elevated permissions.',
          keywords: ['admin', 'settings', 'manage', 'users', 'roles', 'configuration']
        },
        {
          id: 'admin-audit',
          title: 'Viewing audit logs',
          summary: 'See a record of important actions taken in the application.',
          body: 'The Audit Logs page shows a timeline of significant events such as project creation, role changes, and data modifications. Use this to understand who did what and when.',
          keywords: ['audit', 'log', 'history', 'activity', 'trail', 'events']
        }
      ]
    },

    /* ─── Accessibility ─── */
    {
      id: 'accessibility',
      title: 'Accessibility',
      icon: 'M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2 2 6.477 2 12s4.477 10 10 10z M12 6a1.5 1.5 0 100 3 1.5 1.5 0 000-3z M9 20l.94-7H8l1-3h6l1 3h-2.94L15 20',
      articles: [
        {
          id: 'a11y-keyboard',
          title: 'Keyboard navigation',
          summary: 'Use the application without a mouse.',
          body: 'Every feature in Project Pulse can be reached using only a keyboard. Press Tab to move forward through controls, Shift+Tab to move backward, Enter or Space to activate buttons, and Escape to close panels. A "Skip to main content" link appears when you press Tab at the top of any page.',
          keywords: ['keyboard', 'tab', 'enter', 'escape', 'focus', 'shortcut', 'navigation']
        },
        {
          id: 'a11y-screen-reader',
          title: 'Screen reader support',
          summary: 'Project Pulse works with assistive technology.',
          body: 'All controls have descriptive labels, images have alternative text, and dynamic updates are announced automatically. The application follows WCAG 2.1 Level AA guidelines.',
          keywords: ['screen reader', 'assistive', 'wcag', 'aria', 'label', 'alt text']
        },
        {
          id: 'a11y-motion',
          title: 'Reducing motion',
          summary: 'Minimise animations if you prefer less movement on screen.',
          body: 'If your device is set to "reduce motion" (in your operating system accessibility settings), Project Pulse will automatically turn off most animations and transitions.',
          keywords: ['motion', 'animation', 'reduce', 'prefers-reduced-motion', 'flashing']
        }
      ]
    }
  ]
};
