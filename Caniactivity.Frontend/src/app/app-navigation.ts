export const navigation = [
  {
    text: 'Accueil',
    path: '/home',
    icon: 'home'
  },
  {
    text: 'Le parc',
    icon: 'product',
    items: [
      {
        text: 'L\'environnement',
        path: '/pages/environment'
      },
      {
        text: 'Les activités',
        path: '/pages/activities'
      },
      {
        text: 'Tarifs',
        path: '/pages/tarifs'
      }
    ]
  },
  {
    text: 'Réservation',
    path: '/pages/schedule',
    icon: 'clock'
  },
  {
    text: 'Administration',
    icon: 'folder',
    items: [
      {
        text: 'Users',
        path: '/pages/users'
      },
      {
        text: 'Scheduler Admin',
        path: '/pages/scheduler-admin'
      }
    ]
  }
];
