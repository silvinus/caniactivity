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
      }
    ]
  },
  {
    text: 'Réservation',
    path: '/pages/schedule',
    icon: 'clock'
  }
];
